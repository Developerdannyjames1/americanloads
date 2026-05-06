import {
  BadRequestException,
  ForbiddenException,
  Injectable,
  NotFoundException,
} from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { In, Repository } from 'typeorm';
import { AspNetRole, AspNetUser, AspNetUserRole, Company, Load, LoadClaim } from '../entities';
import {
  ClaimStatus,
  ClaimType,
  CompanyType,
  LoadStatus,
  OnboardingStatus,
  Roles,
} from '../common/constants';
import { LoadsService } from '../loads/loads.service';
import { RealtimeGateway } from '../realtime/realtime.gateway';
import { SubmitClaimDto } from './dto';
import { MailService } from '../mail/mail.service';

type Caller = { sub: string; role: string; companyId: number | null };

@Injectable()
export class ClaimsService {
  constructor(
    @InjectRepository(LoadClaim) private readonly claims: Repository<LoadClaim>,
    @InjectRepository(Load) private readonly loads: Repository<Load>,
    @InjectRepository(AspNetUser) private readonly users: Repository<AspNetUser>,
    @InjectRepository(AspNetRole) private readonly roles: Repository<AspNetRole>,
    @InjectRepository(AspNetUserRole) private readonly userRoles: Repository<AspNetUserRole>,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
    private readonly realtime: RealtimeGateway,
    private readonly loadsSvc: LoadsService,
    private readonly mail: MailService,
  ) {}

  private async adminEmails(): Promise<string[]> {
    const adminRole = await this.roles.findOne({ where: { Name: Roles.Admin } });
    if (!adminRole) return [];
    const links = await this.userRoles.find({ where: { RoleId: adminRole.Id } });
    if (links.length === 0) return [];
    const ids = [...new Set(links.map((l) => l.UserId))];
    const users = await this.users.find({ where: { Id: In(ids) } });
    return [...new Set(users.map((u) => (u.Email || '').trim()).filter((e) => !!e))];
  }

  private async notifyAdminsNewSubmission(load: Load, dto: SubmitClaimDto, byUserId: string) {
    const submitter = await this.users.findOne({ where: { Id: byUserId } });
    const kind = dto.claimType === ClaimType.Bid ? 'bid' : 'claim';
    const subject = `New ${kind} on load ${load.PostersReferenceId || `#${load.Id}`}`;
    const submitterName = submitter?.FullName || submitter?.UserName || byUserId;
    const body = `
      <div style="font-family:Arial,Helvetica,sans-serif;line-height:1.5;color:#111">
        <h3 style="margin:0 0 12px">New ${kind} submitted</h3>
        <p><b>Load:</b> ${load.PostersReferenceId || `#${load.Id}`}</p>
        <p><b>Type:</b> ${dto.claimType}</p>
        ${dto.bidAmount != null ? `<p><b>Bid amount:</b> ${dto.bidAmount}</p>` : ''}
        <p><b>Submitted by:</b> ${submitterName}</p>
      </div>
    `;
    const emails = await this.adminEmails();
    await Promise.all(emails.map((to) => this.mail.send(to, subject, body)));
  }

  /** Carriers, or dispatchers whose company is a carrier (not shipper-side dispatchers). */
  private async assertSubmitClaimAllowed(caller: Caller) {
    if (!caller.companyId) throw new BadRequestException('No company for this user');
    const co = await this.companies.findOne({ where: { Id: caller.companyId } });
    if (!co) throw new BadRequestException('Company not found');
    if ((co.OnboardingStatus || '').toLowerCase() !== OnboardingStatus.Approved) {
      throw new ForbiddenException('Company is not approved');
    }
    const t = (co.CompanyType || '').toLowerCase();
    if (caller.role === Roles.Carrier) {
      if (t !== CompanyType.Carrier.toLowerCase()) {
        throw new ForbiddenException('Carrier accounts must belong to a carrier company');
      }
      return;
    }
    if (caller.role === Roles.Dispatcher) {
      if (t === CompanyType.Carrier.toLowerCase()) return;
      throw new ForbiddenException('Only dispatchers at carrier companies can submit claims');
    }
    throw new ForbiddenException('Only carriers and carrier dispatchers can submit');
  }

  /** View claims on a load: admin, or shipper / dispatcher for the load's owning company. */
  private canViewClaimsOnLoad(caller: Caller, load: Load): boolean {
    if (caller.role === Roles.Admin) return true;
    if (caller.role === Roles.Shipper) {
      if (load.ShipperUserId === caller.sub) return true;
      if (caller.companyId != null && load.CompanyId === caller.companyId) return true;
      return false;
    }
    if (caller.role === Roles.Dispatcher && caller.companyId != null && load.CompanyId === caller.companyId) {
      return true;
    }
    return false;
  }

  private async isCarrierSideForMyClaims(caller: Caller): Promise<boolean> {
    if (caller.role === Roles.Carrier) return true;
    if (caller.role !== Roles.Dispatcher || caller.companyId == null) return false;
    const co = await this.companies.findOne({ where: { Id: caller.companyId } });
    return !!co && (co.CompanyType || '').toLowerCase() === CompanyType.Carrier.toLowerCase();
  }

  shape(c: LoadClaim, extras?: { load?: any; carrier?: any }) {
    return {
      id: c.Id,
      _id: String(c.Id),
      loadId: c.LoadId,
      carrierUserId: c.CarrierUserId,
      claimType: c.ClaimType,
      bidAmount: c.BidAmount != null ? Number(c.BidAmount) : null,
      message: c.Message || '',
      status: c.Status,
      createdAt: c.CreatedUtc,
      resolvedAt: c.ResolvedUtc,
      resolvedByUserId: c.ResolvedByUserId,
      load: extras?.load,
      carrier: extras?.carrier,
    };
  }

  async submit(caller: Caller, dto: SubmitClaimDto) {
    await this.assertSubmitClaimAllowed(caller);

    const load = await this.loads.findOne({ where: { Id: dto.loadId } });
    if (!load) throw new NotFoundException('Load not found');
    const isPosted = (load.WorkflowStatus || LoadStatus.Posted).toLowerCase() === LoadStatus.Posted;
    if (!isPosted) throw new BadRequestException('Load is not currently open');

    const existing = await this.claims.findOne({
      where: { LoadId: dto.loadId, CarrierUserId: caller.sub, Status: ClaimStatus.Pending },
    });
    if (existing) throw new BadRequestException('You already have an open submission for this load');

    const claim = this.claims.create({
      LoadId: dto.loadId,
      CarrierUserId: caller.sub,
      ClaimType: dto.claimType === ClaimType.Bid ? ClaimType.Bid : ClaimType.Claim,
      BidAmount: dto.claimType === ClaimType.Bid ? dto.bidAmount ?? null : null,
      Message: dto.message || null,
      Status: ClaimStatus.Pending,
      CreatedUtc: new Date(),
    });
    const saved = await this.claims.save(claim);

    if (load.ShipperUserId) this.realtime.emitToUser(load.ShipperUserId, 'claim_submitted', { id: saved.Id });
    const claimEvent = dto.claimType === ClaimType.Bid ? 'new_bid' : 'new_claim';
    this.realtime.emitToAdmins(claimEvent, {
      id: saved.Id,
      loadId: dto.loadId,
      claimType: dto.claimType,
    });
    this.realtime.broadcast(claimEvent, {
      id: saved.Id,
      loadId: dto.loadId,
      claimType: dto.claimType,
    });
    this.realtime.broadcast('claim_updated', { id: saved.Id });
    await this.notifyAdminsNewSubmission(load, dto, caller.sub).catch(() => {});

    return this.shape(saved);
  }

  async listForLoad(caller: Caller, loadId: number) {
    const load = await this.loads.findOne({ where: { Id: loadId } });
    if (!load) throw new NotFoundException();
    if (!this.canViewClaimsOnLoad(caller, load)) throw new ForbiddenException();
    const rows = await this.claims.find({ where: { LoadId: loadId }, order: { CreatedUtc: 'DESC' } });
    const carrierIds = [...new Set(rows.map((r) => r.CarrierUserId))];
    const carriers = carrierIds.length
      ? await this.users.find({ where: { Id: In(carrierIds) } })
      : [];
    const cMap = new Map(carriers.map((c) => [c.Id, c]));
    return rows.map((r) =>
      this.shape(r, {
        carrier: cMap.get(r.CarrierUserId)
          ? { id: r.CarrierUserId, fullName: cMap.get(r.CarrierUserId)!.FullName, email: cMap.get(r.CarrierUserId)!.Email }
          : null,
      }),
    );
  }

  async myClaims(caller: Caller) {
    if (!(await this.isCarrierSideForMyClaims(caller))) return [];
    const rows = await this.claims.find({
      where: { CarrierUserId: caller.sub },
      order: { CreatedUtc: 'DESC' },
    });
    const loadIds = [...new Set(rows.map((r) => r.LoadId))];
    const loads = loadIds.length ? await this.loads.find({ where: { Id: In(loadIds) } }) : [];
    const lMap = new Map(loads.map((l) => [l.Id, l]));
    return rows.map((r) =>
      this.shape(r, {
        load: lMap.get(r.LoadId)
          ? {
              id: r.LoadId,
              refId: lMap.get(r.LoadId)!.PostersReferenceId || '',
              status: lMap.get(r.LoadId)!.WorkflowStatus || LoadStatus.Posted,
            }
          : null,
      }),
    );
  }

  async listAll(caller: Caller) {
    if (caller.role !== Roles.Admin) throw new ForbiddenException();
    const rows = await this.claims.find({ order: { CreatedUtc: 'DESC' }, take: 500 });
    const loadIds = [...new Set(rows.map((r) => r.LoadId))];
    const carrierIds = [...new Set(rows.map((r) => r.CarrierUserId))];
    const [loads, carriers] = await Promise.all([
      loadIds.length ? this.loads.find({ where: { Id: In(loadIds) } }) : Promise.resolve([] as Load[]),
      carrierIds.length ? this.users.find({ where: { Id: In(carrierIds) } }) : Promise.resolve([] as AspNetUser[]),
    ]);
    const lMap = new Map(loads.map((l) => [l.Id, l]));
    const cMap = new Map(carriers.map((c) => [c.Id, c]));
    return rows.map((r) =>
      this.shape(r, {
        load: lMap.get(r.LoadId)
          ? {
              id: r.LoadId,
              refId: lMap.get(r.LoadId)!.PostersReferenceId || '',
              status: lMap.get(r.LoadId)!.WorkflowStatus || LoadStatus.Posted,
            }
          : null,
        carrier: cMap.get(r.CarrierUserId)
          ? { id: r.CarrierUserId, fullName: cMap.get(r.CarrierUserId)!.FullName, email: cMap.get(r.CarrierUserId)!.Email }
          : null,
      }),
    );
  }

  async accept(caller: Caller, claimId: number) {
    const c = await this.claims.findOne({ where: { Id: claimId } });
    if (!c) throw new NotFoundException();
    if (c.Status !== ClaimStatus.Pending) throw new BadRequestException('Already resolved');
    const load = await this.loads.findOne({ where: { Id: c.LoadId } });
    if (!load) throw new NotFoundException('Load missing');
    if (caller.role !== Roles.Admin) throw new ForbiddenException();

    c.Status = ClaimStatus.Accepted;
    c.ResolvedUtc = new Date();
    c.ResolvedByUserId = caller.sub;
    await this.claims.save(c);

    load.AssignedCarrierUserId = c.CarrierUserId;
    load.WorkflowStatus = LoadStatus.Assigned;
    if (c.ClaimType === ClaimType.Bid && c.BidAmount != null) {
      load.CarrierAmount = Number(c.BidAmount);
    }
    load.UpdateDate = new Date();
    load.UpdatedBy = caller.sub;
    await this.loads.save(load);

    // Reject other pending claims for this load
    await this.claims
      .createQueryBuilder()
      .update()
      .set({ Status: ClaimStatus.Rejected, ResolvedUtc: new Date(), ResolvedByUserId: caller.sub })
      .where('LoadId = :lid AND Status = :pending AND Id <> :id', {
        lid: c.LoadId,
        pending: ClaimStatus.Pending,
        id: c.Id,
      })
      .execute();

    this.realtime.broadcast('load_updated', { id: load.Id, status: load.WorkflowStatus });
    this.realtime.broadcast('claim_updated', { id: c.Id, loadId: load.Id, status: 'accepted' });
    this.realtime.emitToUser(c.CarrierUserId, 'claim_accepted', { id: c.Id, loadId: load.Id });
    return this.shape(c);
  }

  async reject(caller: Caller, claimId: number) {
    const c = await this.claims.findOne({ where: { Id: claimId } });
    if (!c) throw new NotFoundException();
    if (c.Status !== ClaimStatus.Pending) throw new BadRequestException('Already resolved');
    const load = await this.loads.findOne({ where: { Id: c.LoadId } });
    if (!load) throw new NotFoundException('Load missing');
    if (caller.role !== Roles.Admin) throw new ForbiddenException();
    c.Status = ClaimStatus.Rejected;
    c.ResolvedUtc = new Date();
    c.ResolvedByUserId = caller.sub;
    await this.claims.save(c);
    this.realtime.broadcast('claim_updated', { id: c.Id, loadId: c.LoadId, status: 'rejected' });
    this.realtime.emitToUser(c.CarrierUserId, 'claim_rejected', { id: c.Id, loadId: c.LoadId });
    return this.shape(c);
  }
}
