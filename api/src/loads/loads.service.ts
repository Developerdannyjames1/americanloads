import {
  BadRequestException,
  ForbiddenException,
  Injectable,
  NotFoundException,
} from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository, In } from 'typeorm';
import { AspNetUser, Company, Load, LoadType, OriginDestination, State } from '../entities';
import {
  CompanyType,
  LoadStatus,
  ODType,
  OnboardingStatus,
  Roles,
  type Role,
} from '../common/constants';
import { CreateLoadDto, ListLoadsDto, UpdateLoadDto } from './dto';
import { RealtimeGateway } from '../realtime/realtime.gateway';
import { MailService } from '../mail/mail.service';

type Caller = {
  sub: string;
  role: string;
  companyId: number | null;
};

type Place = { city?: string; state?: string; zip?: string };

@Injectable()
export class LoadsService {
  constructor(
    @InjectRepository(Load) private readonly loads: Repository<Load>,
    @InjectRepository(OriginDestination) private readonly ods: Repository<OriginDestination>,
    @InjectRepository(State) private readonly states: Repository<State>,
    @InjectRepository(LoadType) private readonly loadTypes: Repository<LoadType>,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
    @InjectRepository(AspNetUser) private readonly users: Repository<AspNetUser>,
    private readonly realtime: RealtimeGateway,
    private readonly mail: MailService,
  ) {}

  private formatStatusLabel(status: string | null | undefined) {
    return String(status || '')
      .replaceAll('_', ' ')
      .replace(/\b\w/g, (m) => m.toUpperCase());
  }

  private async loadParties(l: Load) {
    const ids = [l.ShipperUserId, l.AssignedCarrierUserId].filter(Boolean) as string[];
    if (ids.length === 0) return { shipper: null as AspNetUser | null, carrier: null as AspNetUser | null };
    const users = await this.users.find({ where: { Id: In([...new Set(ids)]) } });
    const byId = new Map(users.map((u) => [u.Id, u]));
    return {
      shipper: l.ShipperUserId ? byId.get(l.ShipperUserId) ?? null : null,
      carrier: l.AssignedCarrierUserId ? byId.get(l.AssignedCarrierUserId) ?? null : null,
    };
  }

  private async notifyLoadAssigned(l: Load) {
    const parties = await this.loadParties(l);
    const ref = l.PostersReferenceId || `#${l.Id}`;
    if (parties.carrier?.Email) {
      await this.mail.send(
        parties.carrier.Email,
        `Load assigned: ${ref}`,
        `<p>A load has been assigned to you.</p><p><b>Load:</b> ${ref}</p><p><b>Status:</b> Assigned</p>`,
      );
    }
    if (parties.shipper?.Email) {
      await this.mail.send(
        parties.shipper.Email,
        `Carrier assigned to load ${ref}`,
        `<p>Your load has been assigned to a carrier.</p><p><b>Load:</b> ${ref}</p><p><b>Status:</b> Assigned</p>`,
      );
    }
  }

  private async notifyLoadStatusUpdate(l: Load, status: string) {
    const parties = await this.loadParties(l);
    const ref = l.PostersReferenceId || `#${l.Id}`;
    const statusText = this.formatStatusLabel(status);
    const subject = `Load ${ref} status updated: ${statusText}`;
    const html = `<p>Load status was updated.</p><p><b>Load:</b> ${ref}</p><p><b>Status:</b> ${statusText}</p>`;
    const emails = [parties.shipper?.Email, parties.carrier?.Email]
      .map((e) => (e || '').trim())
      .filter((e) => !!e);
    await Promise.all([...new Set(emails)].map((to) => this.mail.send(to, subject, html)));
  }

  async loadTypesList() {
    const rows = await this.loadTypes
      .createQueryBuilder('lt')
      .orderBy('lt.Id', 'ASC')
      .getMany();
    return rows
      .map((lt) => {
        const name =
          String(lt.Name || '').trim() ||
          String(lt.NameDAT || '').trim() ||
          String(lt.NameTS || '').trim() ||
          `Type #${lt.Id}`;
        return { id: lt.Id, name };
      })
      .filter((x) => !!x.name);
  }

  /** Convert a legacy Load row to API shape. Pass `assigned` / `shipperUser` when enriched from AspNetUsers. */
  shape(
    l: Load,
    assigned?: { user: AspNetUser; company: Company | null } | null,
    shipperUser?: AspNetUser | null,
  ) {
    const billed = l.CustomerAmount != null ? Number(l.CustomerAmount) : null;
    const cost = l.CarrierAmount != null ? Number(l.CarrierAmount) : null;
    const profit = billed != null && cost != null ? Number((billed - cost).toFixed(2)) : null;
    const margin = billed && billed > 0 && cost != null ? Number((((billed - cost) / billed) * 100).toFixed(2)) : null;
    const od = (od?: OriginDestination) =>
      od
        ? {
            id: od.Id,
            city: od.City || '',
            state: od.State?.Code || '',
            zip: od.PostalCode || '',
          }
        : { id: null, city: '', state: '', zip: '' };
    const au = assigned?.user;
    const co = assigned?.company ?? null;
    return {
      id: l.Id,
      _id: String(l.Id),
      refId: l.PostersReferenceId || '',
      status: (l.WorkflowStatus || LoadStatus.Draft) as string,
      equipmentType: l.EquipmentType || '',
      trailerLengthFt: l.AssetLength ?? null,
      weightLbs: l.Weight ?? null,
      commodity: l.Commodity || '',
      pickUpDate: l.PickUpDate,
      deliveryDate: l.DeliveryDate,
      loadDate: l.DateLoaded,
      untilDate: l.UntilDate,
      isLoadFull: !!l.IsLoadFull,
      allowUntilSat: l.AllowUntilSat ?? false,
      allowUntilSun: l.AllowUntilSun ?? false,
      billedToCustomer: billed,
      payToCarrier: cost,
      profit,
      marginPercent: margin,
      description: l.Description || '',
      userNotes: l.UserNotes || '',
      notes: l.UserNotes || l.Description || '',
      shipperUserId: l.ShipperUserId || null,
      shipperCompanyId: l.CompanyId ?? null,
      assignedCarrierUserId: l.AssignedCarrierUserId || null,
      assignedCarrier: au
        ? {
            userId: au.Id,
            fullName: au.FullName || au.UserName,
            email: au.Email || au.UserName,
            companyId: au.CompanyId ?? null,
            companyName: co?.Name ?? null,
            companyType: co?.CompanyType ?? null,
          }
        : null,
      shipper:
        l.ShipperUserId || l.CompanyId
          ? {
              userId: l.ShipperUserId || null,
              fullName: shipperUser ? shipperUser.FullName || shipperUser.UserName : null,
              email: shipperUser ? shipperUser.Email || shipperUser.UserName : null,
              companyId: l.CompanyId ?? null,
              companyName: l.Company?.Name ?? null,
              companyType: l.Company?.CompanyType ?? null,
            }
          : null,
      origin: od(l.Origin),
      destination: od(l.Destination),
      loadTypeId: l.LoadTypeId ?? null,
      createdAt: l.CreateDate,
      updatedAt: l.UpdateDate,
    };
  }

  /** Batch-load shipper + assigned carrier users and carrier companies for list/detail responses. */
  private async attachAssignedCarriers(rows: Load[]): Promise<any[]> {
    const carrierIds = [...new Set(rows.map((r) => r.AssignedCarrierUserId).filter(Boolean))] as string[];
    const shipperIds = [...new Set(rows.map((r) => r.ShipperUserId).filter(Boolean))] as string[];
    const userIds = [...new Set([...carrierIds, ...shipperIds])];
    const usersList = userIds.length ? await this.users.find({ where: { Id: In(userIds) } }) : [];
    const uMap = new Map(usersList.map((u) => [u.Id, u]));

    const carrierCompanyIds = [
      ...new Set(carrierIds.map((id) => uMap.get(id)?.CompanyId).filter((x): x is number => x != null)),
    ];
    const companies = carrierCompanyIds.length
      ? await this.companies.find({ where: { Id: In(carrierCompanyIds) } })
      : [];
    const coMap = new Map(companies.map((c) => [c.Id, c]));

    return rows.map((l) => {
      const shipperU = l.ShipperUserId ? uMap.get(l.ShipperUserId) ?? null : null;
      const uid = l.AssignedCarrierUserId;
      if (!uid) return this.shape(l, null, shipperU);
      const u = uMap.get(uid);
      if (!u) return this.shape(l, null, shipperU);
      const cco = u.CompanyId != null ? coMap.get(u.CompanyId) ?? null : null;
      return this.shape(l, { user: u, company: cco }, shipperU);
    });
  }

  private async attachOne(l: Load): Promise<any> {
    const [one] = await this.attachAssignedCarriers([l]);
    return one;
  }

  private baseQuery() {
    return this.loads
      .createQueryBuilder('l')
      .leftJoinAndSelect('l.Origin', 'origin')
      .leftJoinAndSelect('origin.State', 'originState')
      .leftJoinAndSelect('l.Destination', 'destination')
      .leftJoinAndSelect('destination.State', 'destState')
      .leftJoinAndSelect('l.Company', 'company')
      .leftJoinAndSelect('l.LoadType', 'loadType');
  }

  async list(caller: Caller, q: ListLoadsDto) {
    const qb = this.baseQuery();

    if (caller.role === Roles.Admin) {
      // sees all
    } else if (caller.role === Roles.Shipper) {
      qb.andWhere(
        '(l.ShipperUserId = :uid OR l.CompanyId = :cid)',
        { uid: caller.sub, cid: caller.companyId ?? -1 },
      );
    } else if (caller.role === Roles.Carrier) {
      const co = caller.companyId ? await this.companies.findOne({ where: { Id: caller.companyId } }) : null;
      const approved =
        co &&
        co.OnboardingStatus &&
        co.OnboardingStatus.toLowerCase() === OnboardingStatus.Approved;
      if (!approved) return [];
      qb.andWhere(
        '(l.WorkflowStatus = :posted OR l.WorkflowStatus IS NULL OR l.AssignedCarrierUserId = :uid)',
        { posted: LoadStatus.Posted, uid: caller.sub },
      );
    } else if (caller.role === Roles.Dispatcher) {
      if (!caller.companyId) return [];
      qb.andWhere('(l.CompanyId = :cid OR l.AssignedCarrierUserId IN (SELECT Id FROM AspNetUsers WHERE CompanyId = :cid))', {
        cid: caller.companyId,
      });
    } else {
      return [];
    }

    if (q.refId) qb.andWhere('l.PostersReferenceId LIKE :ref', { ref: `%${q.refId}%` });
    if (q.loadTypeId != null) {
      const ltid = Number(q.loadTypeId);
      const lt = Number.isFinite(ltid) ? await this.loadTypes.findOne({ where: { Id: ltid } }) : null;
      const ltName = String(lt?.Name || lt?.NameDAT || lt?.NameTS || '').trim().toLowerCase();
      if (ltName) {
        qb.andWhere(
          `(
            LOWER(LTRIM(RTRIM(COALESCE(l.EquipmentType, :empty)))) = :ltName
            OR LOWER(LTRIM(RTRIM(COALESCE(loadType.Name, :empty)))) = :ltName
            OR LOWER(LTRIM(RTRIM(COALESCE(loadType.NameDAT, :empty)))) = :ltName
            OR LOWER(LTRIM(RTRIM(COALESCE(loadType.NameTS, :empty)))) = :ltName
          )`,
          { ltName, empty: '' },
        );
      } else {
        qb.andWhere('l.LoadTypeId = :ltid', { ltid });
      }
    } else if (q.equipmentType) qb.andWhere('l.EquipmentType LIKE :eq', { eq: `%${q.equipmentType}%` });
    if (q.originId != null) qb.andWhere('l.OriginId = :oid', { oid: q.originId });
    else if (q.origin) qb.andWhere('origin.City LIKE :oc', { oc: `%${q.origin}%` });
    if (q.destinationId != null) qb.andWhere('l.DestinationId = :did', { did: q.destinationId });
    else if (q.destination) qb.andWhere('destination.City LIKE :dc', { dc: `%${q.destination}%` });
    if (q.status) {
      const st = String(q.status || '').trim().toLowerCase();
      if (st === LoadStatus.Draft) {
        qb.andWhere(
          '(l.WorkflowStatus IS NULL OR LTRIM(RTRIM(l.WorkflowStatus)) = :empty OR LOWER(LTRIM(RTRIM(l.WorkflowStatus))) = :draft)',
          { empty: '', draft: LoadStatus.Draft },
        );
      } else {
        qb.andWhere('LOWER(LTRIM(RTRIM(COALESCE(l.WorkflowStatus, :empty)))) = LOWER(:st)', {
          st,
          empty: '',
        });
      }
    }

    qb.orderBy('l.Id', 'DESC').take(500);

    const rows = await qb.getMany();
    return this.attachAssignedCarriers(rows);
  }

  async byId(caller: Caller, id: number) {
    const l = await this.baseQuery().where('l.Id = :id', { id }).getOne();
    if (!l) throw new NotFoundException();
    if (!this.canView(caller, l)) throw new ForbiddenException();
    return this.attachOne(l);
  }

  private canView(caller: Caller, l: Load) {
    if (caller.role === Roles.Admin) return true;
    if (caller.role === Roles.Shipper) {
      if (l.ShipperUserId === caller.sub) return true;
      if (caller.companyId != null && l.CompanyId === caller.companyId) return true;
      return false;
    }
    if (caller.role === Roles.Carrier) {
      const status = (l.WorkflowStatus || LoadStatus.Posted).toLowerCase();
      if (status === LoadStatus.Posted) return true;
      if (l.AssignedCarrierUserId === caller.sub) return true;
      return false;
    }
    if (caller.role === Roles.Dispatcher) {
      if (caller.companyId == null) return false;
      if (l.CompanyId === caller.companyId) return true;
      return false;
    }
    return false;
  }

  /** Find or create an OriginDestination row by city+state+zip. */
  private async findOrCreatePlace(p: Place, type: number): Promise<OriginDestination | null> {
    const city = (p.city || '').trim();
    const stateCode = (p.state || '').trim().toUpperCase();
    const zip = (p.zip || '').trim();
    if (!city && !stateCode && !zip) return null;
    let stateRow: State | null = null;
    if (stateCode) {
      stateRow = await this.states.findOne({ where: { Code: stateCode } });
    }
    if (!stateRow) {
      throw new BadRequestException(`Unknown state code "${stateCode}"`);
    }

    const existing = await this.ods
      .createQueryBuilder('od')
      .where('od.City = :c', { c: city })
      .andWhere('od.StateId = :sid', { sid: stateRow.Id })
      .andWhere(zip ? 'od.PostalCode = :z' : '(od.PostalCode IS NULL OR od.PostalCode = \'\')', { z: zip })
      .orderBy('od.Id', 'ASC')
      .getOne();
    if (existing) {
      existing.State = stateRow;
      return existing;
    }
    const created = this.ods.create({
      Type: type,
      City: city,
      StateId: stateRow.Id,
      PostalCode: zip || null,
      Country: 'USA',
    });
    const saved = await this.ods.save(created);
    saved.State = stateRow;
    return saved;
  }

  /** Every new load must be tied to a shipper company; callers must pass `shipperCompanyId`. */
  private async resolveShipperCompanyId(caller: Caller, dto: CreateLoadDto): Promise<number> {
    const raw = dto.shipperCompanyId;
    if (raw == null || Number.isNaN(Number(raw))) {
      throw new BadRequestException('Shipper company is required');
    }
    const id = Number(raw);
    const co = await this.companies.findOne({ where: { Id: id } });
    if (!co) throw new BadRequestException('Shipper company not found');
    if ((co.CompanyType || '').toLowerCase() !== CompanyType.Shipper.toLowerCase()) {
      throw new BadRequestException('Selected company must be a shipper company');
    }
    if ((co.OnboardingStatus || '').toLowerCase() !== OnboardingStatus.Approved) {
      throw new BadRequestException('Selected shipper company must be approved');
    }
    if (caller.role === Roles.Admin) {
      return id;
    }
    if (!caller.companyId) {
      throw new BadRequestException('Your account must be linked to a shipper company to create loads');
    }
    if (id !== caller.companyId) {
      throw new ForbiddenException('You can only create loads for your shipper company');
    }
    return id;
  }

  private async resolveLoadType(payload: { loadTypeId?: number; equipmentType?: string }) {
    if (payload.loadTypeId == null) return null;
    const lt = await this.loadTypes.findOne({ where: { Id: payload.loadTypeId } });
    if (!lt) throw new BadRequestException('Invalid load type');
    return lt;
  }

  async create(caller: Caller, dto: CreateLoadDto) {
    if (
      caller.role !== Roles.Admin &&
      caller.role !== Roles.Shipper &&
      caller.role !== Roles.Dispatcher
    )
      throw new ForbiddenException('Cannot create loads');

    const companyId = await this.resolveShipperCompanyId(caller, dto);
    const lt = await this.resolveLoadType(dto);
    if (!lt) throw new BadRequestException('Equipment type is required');
    if (!dto.pickUpDate) throw new BadRequestException('Pickup date is required');
    if (!dto.deliveryDate) throw new BadRequestException('Delivery date is required');

    const origin = await this.findOrCreatePlace(dto.origin || {}, ODType.Origin);
    if (!origin) throw new BadRequestException('Origin city + state are required');
    const destination = dto.destination ? await this.findOrCreatePlace(dto.destination, ODType.Destination) : null;
    if (!destination) throw new BadRequestException('Destination city + state are required');

    let description: string | null = null;
    let userNotes: string | null = null;
    if (dto.description !== undefined || dto.userNotes !== undefined) {
      if (dto.description !== undefined) description = (dto.description || '').trim() || null;
      if (dto.userNotes !== undefined) userNotes = (dto.userNotes || '').trim() || null;
    } else if (dto.notes !== undefined) {
      const n = (dto.notes || '').trim() || null;
      description = n;
      userNotes = n;
    }

    const load = this.loads.create({
      PostersReferenceId: dto.refId || null,
      EquipmentType: lt?.Name || dto.equipmentType || 'Dry Van',
      OriginId: origin.Id,
      DestinationId: destination?.Id ?? null,
      LoadTypeId: lt?.Id ?? null,
      ShipperUserId: caller.sub,
      CompanyId: companyId,
      WorkflowStatus: LoadStatus.Draft,
      CarrierAmount: dto.payToCarrier ?? 0,
      CustomerAmount: dto.billedToCustomer ?? null,
      PickUpDate: dto.pickUpDate ? new Date(dto.pickUpDate) : null,
      DeliveryDate: dto.deliveryDate ? new Date(dto.deliveryDate) : null,
      DateLoaded: dto.loadDate ? new Date(dto.loadDate) : new Date(),
      UntilDate: dto.untilDate ? new Date(dto.untilDate) : null,
      AssetLength: dto.trailerLengthFt ?? null,
      Weight: dto.weightLbs ?? null,
      Commodity: dto.commodity || null,
      Description: description,
      UserNotes: userNotes,
      IsLoadFull: !!dto.isLoadFull,
      AllowUntilSat: dto.allowUntilSat ?? false,
      AllowUntilSun: dto.allowUntilSun ?? false,
      RateEateBasedOn: 0,
      CreateDate: new Date(),
      CreatedBy: caller.sub,
      UpdateDate: new Date(),
      UpdatedBy: caller.sub,
    });
    const saved = await this.loads.save(load);
    this.realtime.broadcast('load_updated', {
      id: saved.Id,
      status: saved.WorkflowStatus || LoadStatus.Draft,
      action: 'created',
    });
    return this.byIdInternal(saved.Id);
  }

  async update(caller: Caller, id: number, dto: UpdateLoadDto) {
    const l = await this.loads.findOne({ where: { Id: id } });
    if (!l) throw new NotFoundException();
    const isOwner = l.ShipperUserId === caller.sub;
    const isAdmin = caller.role === Roles.Admin;
    if (!isAdmin && !(caller.role === Roles.Shipper && isOwner))
      throw new ForbiddenException();

    if (dto.origin) {
      const origin = await this.findOrCreatePlace(dto.origin, ODType.Origin);
      if (origin) l.OriginId = origin.Id;
    }
    if (dto.destination) {
      const destination = await this.findOrCreatePlace(dto.destination, ODType.Destination);
      l.DestinationId = destination ? destination.Id : null;
    }
    if (dto.refId !== undefined) l.PostersReferenceId = dto.refId || null;
    if (dto.shipperCompanyId !== undefined) {
      const companyId = await this.resolveShipperCompanyId(caller, dto);
      l.CompanyId = companyId;
    }
    if (dto.loadTypeId !== undefined) {
      if (dto.loadTypeId == null) {
        l.LoadTypeId = null;
      } else {
        const lt = await this.resolveLoadType(dto);
        l.LoadTypeId = lt!.Id;
        l.EquipmentType = lt!.Name;
      }
    } else if (dto.equipmentType !== undefined) {
      l.EquipmentType = dto.equipmentType;
    }
    if (dto.trailerLengthFt !== undefined) l.AssetLength = dto.trailerLengthFt ?? null;
    if (dto.weightLbs !== undefined) l.Weight = dto.weightLbs ?? null;
    if (dto.commodity !== undefined) l.Commodity = dto.commodity || null;
    if (dto.pickUpDate !== undefined) l.PickUpDate = dto.pickUpDate ? new Date(dto.pickUpDate) : null;
    if (dto.deliveryDate !== undefined) l.DeliveryDate = dto.deliveryDate ? new Date(dto.deliveryDate) : null;
    if (dto.loadDate !== undefined) l.DateLoaded = dto.loadDate ? new Date(dto.loadDate) : null;
    if (dto.untilDate !== undefined) l.UntilDate = dto.untilDate ? new Date(dto.untilDate) : null;
    if (dto.isLoadFull !== undefined) l.IsLoadFull = !!dto.isLoadFull;
    if (dto.allowUntilSat !== undefined) l.AllowUntilSat = !!dto.allowUntilSat;
    if (dto.allowUntilSun !== undefined) l.AllowUntilSun = !!dto.allowUntilSun;
    if (dto.description !== undefined || dto.userNotes !== undefined) {
      if (dto.description !== undefined) l.Description = (dto.description || '').trim() || null;
      if (dto.userNotes !== undefined) l.UserNotes = (dto.userNotes || '').trim() || null;
    } else if (dto.notes !== undefined) {
      const n = (dto.notes || '').trim() || null;
      l.Description = n;
      l.UserNotes = n;
    }
    if (dto.billedToCustomer !== undefined) l.CustomerAmount = dto.billedToCustomer ?? null;
    // Only admin can set carrier pay
    if (isAdmin && dto.payToCarrier !== undefined) l.CarrierAmount = dto.payToCarrier ?? 0;

    if (!l.LoadTypeId) throw new BadRequestException('Equipment type is required');
    if (!l.PickUpDate) throw new BadRequestException('Pickup date is required');
    if (!l.DeliveryDate) throw new BadRequestException('Delivery date is required');
    if (!l.DestinationId) throw new BadRequestException('Destination city + state are required');

    l.UpdateDate = new Date();
    l.UpdatedBy = caller.sub;
    await this.loads.save(l);
    this.realtime.broadcast('load_updated', {
      id,
      status: l.WorkflowStatus || LoadStatus.Draft,
      action: 'updated',
    });
    return this.byIdInternal(id);
  }

  async setStatus(caller: Caller, id: number, status: string) {
    const l = await this.loads.findOne({ where: { Id: id } });
    if (!l) throw new NotFoundException();
    const isAdmin = caller.role === Roles.Admin;
    const isShipperOwner = caller.role === Roles.Shipper && l.ShipperUserId === caller.sub;
    const isAssignedCarrier = caller.role === Roles.Carrier && l.AssignedCarrierUserId === caller.sub;
    let canExecAsCarrierDispatcher = false;
    if (
      !isAssignedCarrier &&
      caller.role === Roles.Dispatcher &&
      caller.companyId != null &&
      l.AssignedCarrierUserId
    ) {
      const assigned = await this.users.findOne({ where: { Id: l.AssignedCarrierUserId } });
      canExecAsCarrierDispatcher = !!(assigned && assigned.CompanyId === caller.companyId);
    }
    const canAdvanceCarrierLeg = isAssignedCarrier || canExecAsCarrierDispatcher;
    const next = status as string;

    if (next === LoadStatus.Completed) {
      if (!isAdmin) {
        throw new ForbiddenException('Only administrators can mark a load completed');
      }
    } else if (next === LoadStatus.InTransit || next === LoadStatus.Delivered) {
      if (!isAdmin && !canAdvanceCarrierLeg) {
        throw new ForbiddenException('Not allowed to set this status');
      }
    } else if (next === LoadStatus.Cancelled) {
      if (!isAdmin && !isShipperOwner) throw new ForbiddenException('Cannot cancel this load');
    } else if (next === LoadStatus.Posted || next === LoadStatus.Draft) {
      if (!isAdmin && !isShipperOwner) throw new ForbiddenException();
    } else {
      if (!isAdmin) throw new ForbiddenException();
    }
    l.WorkflowStatus = next;
    l.UpdateDate = new Date();
    l.UpdatedBy = caller.sub;
    await this.loads.save(l);
    this.realtime.broadcast('load_updated', { id, status: next, action: 'status_changed' });
    const parties = await this.loadParties(l);
    if (parties.shipper?.Id) {
      this.realtime.emitToUser(parties.shipper.Id, 'load_updated', { id, status: next, action: 'status_changed' });
    }
    if (parties.carrier?.Id) {
      this.realtime.emitToUser(parties.carrier.Id, 'load_updated', { id, status: next, action: 'status_changed' });
    }
    await this.notifyLoadStatusUpdate(l, next).catch(() => {});
    return this.byIdInternal(id);
  }

  async assignCarrier(caller: Caller, loadId: number, carrierUserId: string) {
    if (caller.role !== Roles.Admin) throw new ForbiddenException();
    const l = await this.loads.findOne({ where: { Id: loadId } });
    if (!l) throw new NotFoundException();
    const carrier = await this.users.findOne({ where: { Id: carrierUserId } });
    if (!carrier) throw new BadRequestException('Carrier user not found');
    l.AssignedCarrierUserId = carrierUserId;
    l.WorkflowStatus = LoadStatus.Assigned;
    l.UpdateDate = new Date();
    l.UpdatedBy = caller.sub;
    await this.loads.save(l);
    this.realtime.emitToUser(carrierUserId, 'load_assigned', { id: loadId });
    this.realtime.broadcast('load_updated', { id: loadId, status: LoadStatus.Assigned, action: 'assigned' });
    if (l.ShipperUserId) {
      this.realtime.emitToUser(l.ShipperUserId, 'load_updated', {
        id: loadId,
        status: LoadStatus.Assigned,
        action: 'assigned',
      });
    }
    await this.notifyLoadAssigned(l).catch(() => {});
    return this.byIdInternal(loadId);
  }

  async duplicate(caller: Caller, id: number) {
    const src = await this.byIdInternal(id);
    if (!src) throw new NotFoundException();
    const dto: CreateLoadDto = {
      shipperCompanyId: (src as any).shipper?.companyId ?? (src as any).shipperCompanyId ?? undefined,
      equipmentType: src.equipmentType,
      trailerLengthFt: src.trailerLengthFt ?? undefined,
      weightLbs: src.weightLbs ?? undefined,
      commodity: src.commodity,
      billedToCustomer: src.billedToCustomer ?? undefined,
      payToCarrier: src.payToCarrier ?? undefined,
      origin: { city: src.origin.city, state: src.origin.state, zip: src.origin.zip },
      destination: { city: src.destination.city, state: src.destination.state, zip: src.destination.zip },
      description: src.description,
      userNotes: src.userNotes,
      loadTypeId: src.loadTypeId ?? undefined,
    };
    return this.create(caller, dto);
  }

  async remove(caller: Caller, id: number) {
    const l = await this.loads.findOne({ where: { Id: id } });
    if (!l) throw new NotFoundException();
    const isAdmin = caller.role === Roles.Admin;
    const isShipperOwner = caller.role === Roles.Shipper && l.ShipperUserId === caller.sub;
    if (!isAdmin && !isShipperOwner) throw new ForbiddenException();
    await this.loads.remove(l);
    return { ok: true };
  }

  /** Internal helper: re-fetch a load by id w/ relations and shape it. */
  async byIdInternal(id: number) {
    const l = await this.baseQuery().where('l.Id = :id', { id }).getOne();
    if (!l) return null;
    return this.attachOne(l);
  }
}
