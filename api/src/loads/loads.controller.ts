import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  Patch,
  Post,
  Query,
  UseGuards,
} from '@nestjs/common';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { CurrentUser } from '../common/decorators/current-user.decorator';
import { LoadsService } from './loads.service';
import {
  AssignCarrierDto,
  CreateLoadDto,
  ListLoadsDto,
  SetStatusDto,
  UpdateLoadDto,
} from './dto';

@UseGuards(JwtAuthGuard)
@Controller('loads')
export class LoadsController {
  constructor(private readonly svc: LoadsService) {}

  @Get('types')
  types() {
    return this.svc.loadTypesList();
  }

  @Get()
  list(@CurrentUser() user: any, @Query() q: ListLoadsDto) {
    return this.svc.list(user, q);
  }

  @Get(':id')
  byId(@CurrentUser() user: any, @Param('id') id: string) {
    return this.svc.byId(user, parseInt(id, 10));
  }

  @Post()
  create(@CurrentUser() user: any, @Body() dto: CreateLoadDto) {
    return this.svc.create(user, dto);
  }

  @Patch(':id')
  update(@CurrentUser() user: any, @Param('id') id: string, @Body() dto: UpdateLoadDto) {
    return this.svc.update(user, parseInt(id, 10), dto);
  }

  @Post(':id/duplicate')
  duplicate(@CurrentUser() user: any, @Param('id') id: string) {
    return this.svc.duplicate(user, parseInt(id, 10));
  }

  @Patch(':id/status')
  setStatus(
    @CurrentUser() user: any,
    @Param('id') id: string,
    @Body() dto: SetStatusDto,
  ) {
    return this.svc.setStatus(user, parseInt(id, 10), dto.status);
  }

  @Patch(':id/assign')
  assign(
    @CurrentUser() user: any,
    @Param('id') id: string,
    @Body() dto: AssignCarrierDto,
  ) {
    return this.svc.assignCarrier(user, parseInt(id, 10), dto.carrierUserId);
  }

  @Delete(':id')
  remove(@CurrentUser() user: any, @Param('id') id: string) {
    return this.svc.remove(user, parseInt(id, 10));
  }
}
