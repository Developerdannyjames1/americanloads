import {
  BadRequestException,
  ConflictException,
  Injectable,
  Logger,
  UnauthorizedException,
} from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { JwtService } from '@nestjs/jwt';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import * as crypto from 'crypto';
import { AspNetRole, AspNetUser, AspNetUserRole, Company } from '../entities';
import {
  CompanyType,
  OnboardingStatus,
  Roles,
  pickPrimaryRole,
} from '../common/constants';
import { hashAspNetPasswordV2, verifyAspNetPassword } from '../common/identity-hasher';
import { assertPasswordPolicy } from '../common/password-policy';
import { MailService } from '../mail/mail.service';
import {
  ForgotPasswordDto,
  LoginDto,
  RegisterDto,
  ResetPasswordDto,
} from './dto';

export type SessionUser = {
  sub: string;
  email: string;
  username: string;
  fullName: string;
  role: string;
  companyId: number | null;
  companyPermissions?: {
    canCreateLoads: boolean;
    canSubmitClaims: boolean;
    canAccessCarrierPortal: boolean;
  };
};

type ResetTokenPayload = {
  sub: string;
  email: string;
  purpose: 'password_reset';
  stamp: string;
};

@Injectable()
export class AuthService {
  private readonly logger = new Logger(AuthService.name);

  constructor(
    @InjectRepository(AspNetUser) private readonly users: Repository<AspNetUser>,
    @InjectRepository(AspNetRole) private readonly roles: Repository<AspNetRole>,
    @InjectRepository(AspNetUserRole) private readonly userRoles: Repository<AspNetUserRole>,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
    private readonly jwt: JwtService,
    private readonly cfg: ConfigService,
    private readonly mail: MailService,
  ) {}

  async getRolesForUser(userId: string): Promise<string[]> {
    const rows = await this.userRoles
      .createQueryBuilder('ur')
      .innerJoin(AspNetRole, 'r', 'r.Id = ur.RoleId')
      .where('ur.UserId = :uid', { uid: userId })
      .select('r.Name', 'name')
      .getRawMany<{ name: string }>();
    return rows.map((r) => r.name);
  }

  async loginUser(username: string, password: string) {
    const normalized = (username || '').trim().toLowerCase();
    const user = await this.users
      .createQueryBuilder('u')
      .where('LOWER(u.UserName) = :u', { u: normalized })
      .getOne();
    if (!user || !user.PasswordHash) throw new UnauthorizedException('Invalid username or password');
    const ok = verifyAspNetPassword(user.PasswordHash, password);
    if (!ok) throw new UnauthorizedException('Invalid username or password');

    const roleNames = await this.getRolesForUser(user.Id);
    const role = pickPrimaryRole(roleNames);
    const company =
      user.CompanyId != null ? await this.companies.findOne({ where: { Id: user.CompanyId } }) : null;
    const inherited = this.companyPermissions(company);

    const token = await this.jwt.signAsync({
      sub: user.Id,
      email: user.Email || user.UserName,
      username: user.UserName,
      fullName: user.FullName || user.UserName,
      role,
      companyId: user.CompanyId ?? null,
      companyPermissions: inherited,
    } as SessionUser);

    return {
      token,
      user: this.publicUser(user, role),
    };
  }

  publicUser(u: AspNetUser, role: string) {
    return {
      id: u.Id,
      email: u.Email || u.UserName,
      username: u.UserName,
      fullName: u.FullName || u.UserName,
      role: role.toLowerCase(),
      companyId: u.CompanyId ?? null,
    };
  }

  publicCompany(c: Company | null | undefined) {
    if (!c) return null;
    return {
      id: c.Id,
      name: c.Name,
      companyType: c.CompanyType,
      onboardingStatus: c.OnboardingStatus,
    };
  }

  companyPermissions(c: Company | null | undefined) {
    const type = String(c?.CompanyType || '').toLowerCase();
    const approved = String(c?.OnboardingStatus || '').toLowerCase() === OnboardingStatus.Approved;
    const isShipper = type === CompanyType.Shipper.toLowerCase();
    const isCarrier = type === CompanyType.Carrier.toLowerCase();
    return {
      canCreateLoads: isShipper,
      canSubmitClaims: isCarrier && approved,
      canAccessCarrierPortal: isCarrier && approved,
    };
  }

  async register(dto: RegisterDto) {
    const usernameRaw = dto.username.trim();
    const emailRaw = dto.email.trim().toLowerCase();
    if (!usernameRaw || !dto.password || !dto.companyName?.trim()) throw new BadRequestException('Missing fields');
    assertPasswordPolicy(dto.password);

    const dup = await this.users
      .createQueryBuilder('u')
      .where('LOWER(u.UserName) = :un OR LOWER(LTRIM(RTRIM(u.Email))) = :em', {
        un: usernameRaw.toLowerCase(),
        em: emailRaw,
      })
      .getOne();
    if (dup) throw new ConflictException('Username or email already in use');

    const companyType =
      dto.companyType.toLowerCase() === 'carrier' ? CompanyType.Carrier : CompanyType.Shipper;

    const co = this.companies.create({
      Name: dto.companyName.trim(),
      CompanyType: companyType,
      OnboardingStatus: OnboardingStatus.Pending,
      CreatedUtc: new Date(),
    });
    const savedCo = await this.companies.save(co);

    const newId = crypto.randomUUID();
    const passwordHash = hashAspNetPasswordV2(dto.password);
    const user = this.users.create({
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
      FullName: dto.fullName?.trim() || usernameRaw,
      Location: dto.location?.trim() || null,
      Extension: dto.extension?.trim() || null,
      CompanyId: savedCo.Id,
      CarrierApprovalStatus: companyType === CompanyType.Carrier ? OnboardingStatus.Pending : null,
    });
    await this.users.save(user);

    // Assign role
    const roleName = companyType === CompanyType.Carrier ? Roles.Carrier : Roles.Shipper;
    let role = await this.roles.findOne({ where: { Name: roleName } });
    if (!role) {
      role = this.roles.create({ Id: crypto.randomUUID(), Name: roleName });
      await this.roles.save(role);
    }
    await this.userRoles.save({ UserId: newId, RoleId: role.Id });

    return { user: this.publicUser(user, roleName), company: this.publicCompany(savedCo) };
  }

  async forgot(_dto: ForgotPasswordDto) {
    const email = (_dto.email || '').trim().toLowerCase();
    if (!email) return { ok: true };

    const user = await this.users
      .createQueryBuilder('u')
      .where("LOWER(LTRIM(RTRIM(COALESCE(u.Email, '')))) = :em", { em: email })
      .getOne();

    // Always return ok to avoid leaking whether the email exists.
    if (!user) return { ok: true };

    const tokenExpiresIn = this.cfg.get<string>('RESET_TOKEN_EXPIRES_IN') || '30m';
    const stamp = user.SecurityStamp || '';
    const token = await this.jwt.signAsync(
      {
        sub: user.Id,
        email,
        purpose: 'password_reset',
        stamp,
      } as ResetTokenPayload,
      { expiresIn: tokenExpiresIn },
    );

    const web = this.cfg.get<string>('WEB_ORIGIN') || 'http://localhost:3000';
    const resetUrl = `${web}/reset-password?token=${encodeURIComponent(token)}&email=${encodeURIComponent(email)}`;
    const name = user.FullName || user.UserName || email;
    const html = `
      <div style="font-family:Arial,Helvetica,sans-serif;line-height:1.5;color:#111">
        <h2 style="margin:0 0 12px">Reset your password</h2>
        <p>Hi ${name},</p>
        <p>We received a request to reset your americanloads password.</p>
        <p style="margin:18px 0">
          <a href="${resetUrl}" style="background:#111827;color:#fff;padding:10px 14px;border-radius:6px;text-decoration:none">Reset password</a>
        </p>
        <p>If the button does not work, copy this link into your browser:</p>
        <p><a href="${resetUrl}">${resetUrl}</a></p>
        <p>This link expires in ${tokenExpiresIn}.</p>
        <p>If you did not request this, you can ignore this email.</p>
      </div>
    `;
    await this.mail.send(email, 'americanloads password reset', html);
    return { ok: true };
  }

  async reset(_dto: ResetPasswordDto) {
    const email = (_dto.email || '').trim().toLowerCase();
    const token = (_dto.token || '').trim();
    const password = _dto.password || '';
    if (!email || !token || !password) throw new BadRequestException('Missing token, email, or password');
    assertPasswordPolicy(password);

    let payload: ResetTokenPayload;
    try {
      payload = await this.jwt.verifyAsync<ResetTokenPayload>(token);
    } catch {
      throw new BadRequestException('Invalid or expired reset token');
    }
    if (payload.purpose !== 'password_reset') throw new BadRequestException('Invalid reset token');
    if ((payload.email || '').toLowerCase() !== email) throw new BadRequestException('Reset token does not match email');

    const user = await this.users.findOne({ where: { Id: payload.sub } });
    if (!user) throw new BadRequestException('Invalid reset token');
    const currentEmail = (user.Email || '').trim().toLowerCase();
    if (currentEmail !== email) throw new BadRequestException('Reset token does not match current account email');
    if ((user.SecurityStamp || '') !== (payload.stamp || '')) {
      throw new BadRequestException('Reset token is no longer valid');
    }

    user.PasswordHash = hashAspNetPasswordV2(password);
    user.SecurityStamp = crypto.randomUUID();
    await this.users.save(user);
    return { ok: true };
  }
}
