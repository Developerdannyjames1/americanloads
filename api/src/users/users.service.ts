import {
  BadRequestException,
  ConflictException,
  Injectable,
  NotFoundException,
} from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { In, Repository } from 'typeorm';
import * as crypto from 'crypto';
import { AspNetRole, AspNetUser, AspNetUserRole, Company, Load, LoadClaim } from '../entities';
import {
  CompanyType,
  OnboardingStatus,
  Roles,
  normalizeRole,
  pickPrimaryRole,
  type Role,
} from '../common/constants';
import { hashAspNetPasswordV2 } from '../common/identity-hasher';
import { assertPasswordPolicy } from '../common/password-policy';
import { UpdateUserDto } from './dto/update-user.dto';
import { CreateUserDto } from './dto/create-user.dto';

@Injectable()
export class UsersService {
  constructor(
    @InjectRepository(AspNetUser) private readonly users: Repository<AspNetUser>,
    @InjectRepository(AspNetRole) private readonly roles: Repository<AspNetRole>,
    @InjectRepository(AspNetUserRole) private readonly userRoles: Repository<AspNetUserRole>,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
    @InjectRepository(Load) private readonly loads: Repository<Load>,
    @InjectRepository(LoadClaim) private readonly loadClaims: Repository<LoadClaim>,
  ) {}

  private async roleNamesForUser(userId: string): Promise<string[]> {
    const links = await this.userRoles.find({ where: { UserId: userId } });
    if (links.length === 0) return [];
    const roleIds = [...new Set(links.map((l) => l.RoleId))];
    const roleRows = await this.roles.find({ where: { Id: In(roleIds) } });
    const byId = new Map(roleRows.map((r) => [r.Id, r.Name]));
    return links.map((l) => byId.get(l.RoleId)).filter(Boolean) as string[];
  }

  private async toRow(u: AspNetUser) {
    const roleNames = await this.roleNamesForUser(u.Id);
    const role = pickPrimaryRole(roleNames);
    return {
      id: u.Id,
      username: u.UserName,
      email: u.Email || u.UserName,
      fullName: u.FullName || u.UserName,
      location: u.Location || null,
      extension: u.Extension || null,
      companyId: u.CompanyId ?? null,
      roles: roleNames,
      role: role.toLowerCase(),
      isActive: !u.LockoutEndDateUtc || u.LockoutEndDateUtc < new Date(),
      carrierApprovalStatus: u.CarrierApprovalStatus || null,
      createdAt: null,
    };
  }

  async list() {
    const users = await this.users.find({ order: { Email: 'ASC' } });
    if (users.length === 0) return [];
    return Promise.all(users.map((u) => this.toRow(u)));
  }

  async create(dto: CreateUserDto) {
    assertPasswordPolicy(dto.password);
    const usernameRaw = dto.username.trim();
    const emailRaw = dto.email.trim().toLowerCase();
    if (!usernameRaw) throw new BadRequestException('Username is required');

    const dup = await this.users
      .createQueryBuilder('u')
      .where(
        "LOWER(u.UserName) = :un OR LOWER(LTRIM(RTRIM(COALESCE(u.Email, '')))) = :em",
        {
          un: usernameRaw.toLowerCase(),
          em: emailRaw,
        },
      )
      .getOne();
    if (dup) throw new ConflictException('Username or email already in use');

    const companyId =
      dto.companyId === undefined || dto.companyId === null ? null : dto.companyId;
    let companyRow: Company | null = null;
    if (companyId != null) {
      companyRow = await this.companies.findOne({ where: { Id: companyId } });
      if (!companyRow) throw new BadRequestException('Company not found');
    }

    const r = normalizeRole(dto.role);
    if (!r) throw new BadRequestException('Invalid role');

    let carrierApprovalStatus: string | null =
      dto.carrierApprovalStatus === undefined ? null : dto.carrierApprovalStatus;
    if (dto.carrierApprovalStatus === undefined) {
      const companyIsCarrier =
        !!companyRow &&
        String(companyRow.CompanyType || '').toLowerCase() === CompanyType.Carrier.toLowerCase();
      if (r === Roles.Carrier || (r === Roles.Dispatcher && companyIsCarrier)) {
        carrierApprovalStatus = OnboardingStatus.Pending;
      }
    }

    const newId = crypto.randomUUID();
    const passwordHash = hashAspNetPasswordV2(dto.password);
    const ext =
      dto.extension === undefined ? null : dto.extension === null ? null : String(dto.extension).trim() || null;

    const entity = this.users.create({
      Id: newId,
      Email: emailRaw,
      UserName: usernameRaw,
      EmailConfirmed: true,
      PasswordHash: passwordHash,
      SecurityStamp: crypto.randomUUID(),
      PhoneNumberConfirmed: false,
      TwoFactorEnabled: false,
      LockoutEnabled: true,
      AccessFailedCount: 0,
      LockoutEndDateUtc: null,
      FullName: dto.fullName.trim(),
      Location: dto.location.trim(),
      Extension: ext,
      CompanyId: companyId,
      CarrierApprovalStatus: carrierApprovalStatus,
    });
    await this.users.save(entity);
    await this.setSingleRole(newId, r);
    return this.toRow(entity);
  }

  async setActive(id: string, isActive: boolean) {
    const u = await this.users.findOne({ where: { Id: id } });
    if (!u) throw new NotFoundException();
    u.LockoutEndDateUtc = isActive ? null : new Date(Date.now() + 1000 * 60 * 60 * 24 * 365 * 100);
    await this.users.save(u);
    return this.toRow(u);
  }

  private async setSingleRole(userId: string, roleName: Role) {
    await this.userRoles.delete({ UserId: userId });
    let role = await this.roles.findOne({ where: { Name: roleName } });
    if (!role) {
      role = this.roles.create({ Id: crypto.randomUUID(), Name: roleName });
      await this.roles.save(role);
    }
    await this.userRoles.save({ UserId: userId, RoleId: role.Id });
  }

  async update(targetId: string, dto: UpdateUserDto) {
    const u = await this.users.findOne({ where: { Id: targetId } });
    if (!u) throw new NotFoundException();

    if (dto.email !== undefined) {
      if (dto.email == null || String(dto.email).trim() === '') {
        throw new BadRequestException('Email is required');
      }
      const em = String(dto.email).trim().toLowerCase();
      const dup = await this.users
        .createQueryBuilder('x')
        .where(
          "x.Id <> :id AND LOWER(LTRIM(RTRIM(COALESCE(x.Email, '')))) = :em",
          { id: targetId, em },
        )
        .getOne();
      if (dup) throw new ConflictException('Email already in use');
      u.Email = em;
    }

    if (dto.fullName !== undefined) {
      u.FullName = dto.fullName == null ? null : String(dto.fullName).trim() || null;
    }
    if (dto.location !== undefined) {
      u.Location = dto.location == null ? null : String(dto.location).trim() || null;
    }
    if (dto.extension !== undefined) {
      u.Extension = dto.extension == null ? null : String(dto.extension).trim() || null;
    }

    if (dto.companyId !== undefined) {
      if (dto.companyId === null) {
        u.CompanyId = null;
      } else {
        const co = await this.companies.findOne({ where: { Id: dto.companyId } });
        if (!co) throw new BadRequestException('Company not found');
        u.CompanyId = dto.companyId;
      }
    }

    if (dto.carrierApprovalStatus !== undefined) {
      u.CarrierApprovalStatus = dto.carrierApprovalStatus;
    }

    if (dto.password) {
      assertPasswordPolicy(dto.password);
      u.PasswordHash = hashAspNetPasswordV2(dto.password);
      u.SecurityStamp = crypto.randomUUID();
    }

    await this.users.save(u);

    if (dto.role !== undefined) {
      const r = normalizeRole(dto.role);
      if (!r) throw new BadRequestException('Invalid role');
      await this.setSingleRole(u.Id, r);
    }

    return this.toRow(u);
  }

  async remove(adminUserId: string, targetId: string) {
    if (adminUserId === targetId) {
      throw new BadRequestException('You cannot delete your own account');
    }

    const u = await this.users.findOne({ where: { Id: targetId } });
    if (!u) throw new NotFoundException();

    const loadRefs = await this.loads
      .createQueryBuilder('l')
      .where('l.ShipperUserId = :id OR l.AssignedCarrierUserId = :id', { id: targetId })
      .getCount();
    if (loadRefs > 0) {
      throw new BadRequestException(
        'This user is still referenced by loads (shipper or assigned carrier). Reassign loads first.',
      );
    }

    const claimRefs = await this.loadClaims.count({ where: { CarrierUserId: targetId } });
    if (claimRefs > 0) {
      throw new BadRequestException(
        'This user still has claims in the database. Resolve or migrate those rows first.',
      );
    }

    await this.userRoles.delete({ UserId: targetId });
    await this.users.delete(targetId);
    return { ok: true };
  }
}
