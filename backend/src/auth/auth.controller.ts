import { Body, Controller, Get, Post, Res, UseGuards } from '@nestjs/common';
import type { Response } from 'express';
import { ConfigService } from '@nestjs/config';
import { AuthService } from './auth.service';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { CurrentUser } from '../common/decorators/current-user.decorator';
import {
  ForgotPasswordDto,
  LoginDto,
  RegisterDto,
  ResetPasswordDto,
} from './dto';
import { InjectRepository } from '@nestjs/typeorm';
import { Company } from '../entities';
import { Repository } from 'typeorm';
import { LocationsService } from '../locations/locations.service';

@Controller('auth')
export class AuthController {
  constructor(
    private readonly auth: AuthService,
    private readonly cfg: ConfigService,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
    private readonly locations: LocationsService,
  ) {}

  @Post('register')
  register(@Body() dto: RegisterDto) {
    return this.auth.register(dto);
  }

  /** Public signup helper: office sites from dbo.Locations (AspNetUsers.Location). */
  @Get('register-locations')
  async registerLocations() {
    const rows = await this.locations.listWorkSites();
    return rows.map((r) => ({
      id: r.id,
      code: r.location,
      name: '',
    }));
  }

  @Post('login')
  async login(@Body() dto: LoginDto, @Res({ passthrough: true }) res: Response) {
    const { token, user } = await this.auth.loginUser(dto.username, dto.password);
    res.cookie(this.cfg.get<string>('COOKIE_NAME') || 'al_token', token, {
      httpOnly: true,
      sameSite: 'lax',
      secure: this.cfg.get<string>('COOKIE_SECURE') === 'true',
      domain: this.cfg.get<string>('COOKIE_DOMAIN') || undefined,
      maxAge: 24 * 60 * 60 * 1000,
      path: '/',
    });
    return { user, token };
  }

  @Post('logout')
  logout(@Res({ passthrough: true }) res: Response) {
    res.clearCookie(this.cfg.get<string>('COOKIE_NAME') || 'al_token', { path: '/' });
    return { ok: true };
  }

  @Post('forgot')
  forgot(@Body() dto: ForgotPasswordDto) {
    return this.auth.forgot(dto);
  }

  @Post('reset')
  reset(@Body() dto: ResetPasswordDto) {
    return this.auth.reset(dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('me')
  async me(@CurrentUser() user: any) {
    let company: any = null;
    let companyPermissions = this.auth.companyPermissions(null);
    if (user.companyId != null) {
      const c = await this.companies.findOne({ where: { Id: user.companyId } });
      company = this.auth.publicCompany(c);
      companyPermissions = this.auth.companyPermissions(c);
    }
    return {
      user: {
        sub: user.sub,
        email: user.email,
        username: user.username,
        fullName: user.fullName,
        role: typeof user.role === 'string' ? user.role.toLowerCase() : user.role,
        companyId: user.companyId,
        // Always recompute from live company record so admin status changes apply immediately.
        companyPermissions,
      },
      company,
    };
  }
}
