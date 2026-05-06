import { Controller, Get, Param, Patch, Query, UseGuards } from '@nestjs/common';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { RolesGuard } from '../common/guards/roles.guard';
import { RequireRoles } from '../common/decorators/roles.decorator';
import { Roles } from '../common/constants';
import { CompaniesService } from './companies.service';

@UseGuards(JwtAuthGuard)
@Controller('companies')
export class CompaniesController {
  constructor(private readonly svc: CompaniesService) {}

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Get()
  list(@Query('type') type?: string, @Query('status') status?: string, @Query('q') q?: string) {
    return this.svc.list({ type, status, search: q });
  }

  @Get(':id')
  byId(@Param('id') id: string) {
    return this.svc.byId(parseInt(id, 10));
  }

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Patch(':id/status/:status')
  setStatus(@Param('id') id: string, @Param('status') status: string) {
    return this.svc.setStatus(parseInt(id, 10), status);
  }
}
