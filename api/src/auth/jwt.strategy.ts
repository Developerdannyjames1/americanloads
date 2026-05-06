import { Injectable, UnauthorizedException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { PassportStrategy } from '@nestjs/passport';
import { ExtractJwt, Strategy } from 'passport-jwt';
import { Request } from 'express';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { AspNetUser } from '../entities';
import { normalizeRole } from '../common/constants';

const cookieName = process.env.COOKIE_NAME || 'al_token';

export type JwtPayload = {
  sub: string;
  email: string;
  username?: string;
  fullName: string;
  role: string;
  companyId: number | null;
};

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor(
    cfg: ConfigService,
    @InjectRepository(AspNetUser) private readonly users: Repository<AspNetUser>,
  ) {
    super({
      jwtFromRequest: ExtractJwt.fromExtractors([
        (req: Request) => req?.cookies?.[cookieName] || null,
        ExtractJwt.fromAuthHeaderAsBearerToken(),
      ]),
      ignoreExpiration: false,
      secretOrKey: cfg.get<string>('JWT_SECRET') || 'dev-secret-change',
    });
  }

  async validate(payload: JwtPayload) {
    const u = await this.users.findOne({ where: { Id: payload.sub } });
    if (!u) throw new UnauthorizedException('Inactive user');
    const role = normalizeRole(payload.role);
    if (!role) throw new UnauthorizedException('Invalid token role');
    return {
      sub: u.Id,
      email: u.Email || u.UserName,
      username: u.UserName || payload.username,
      role,
      companyId: u.CompanyId ?? null,
      fullName: u.FullName || u.UserName,
    };
  }
}
