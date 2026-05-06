import { Body, Controller, Get, Param, Patch, Post, Query, UseGuards } from '@nestjs/common';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { CurrentUser } from '../common/decorators/current-user.decorator';
import { ClaimsService } from './claims.service';
import { SubmitClaimDto } from './dto';

@UseGuards(JwtAuthGuard)
@Controller('claims')
export class ClaimsController {
  constructor(private readonly svc: ClaimsService) {}

  @Post()
  submit(@CurrentUser() user: any, @Body() dto: SubmitClaimDto) {
    return this.svc.submit(user, dto);
  }

  @Get()
  list(@CurrentUser() user: any, @Query('loadId') loadId?: string) {
    if (loadId) return this.svc.listForLoad(user, parseInt(loadId, 10));
    return this.svc.listAll(user);
  }

  @Get('mine')
  mine(@CurrentUser() user: any) {
    return this.svc.myClaims(user);
  }

  @Patch(':id/accept')
  accept(@CurrentUser() user: any, @Param('id') id: string) {
    return this.svc.accept(user, parseInt(id, 10));
  }

  @Patch(':id/reject')
  reject(@CurrentUser() user: any, @Param('id') id: string) {
    return this.svc.reject(user, parseInt(id, 10));
  }
}
