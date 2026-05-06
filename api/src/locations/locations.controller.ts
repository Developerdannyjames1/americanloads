import { Controller, Get, Query, UseGuards } from '@nestjs/common';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { LocationsService } from './locations.service';

@UseGuards(JwtAuthGuard)
@Controller('locations')
export class LocationsController {
  constructor(private readonly svc: LocationsService) {}

  @Get('states')
  states() {
    return this.svc.listStates();
  }

  @Get('cities')
  cities(@Query('stateCode') stateCode?: string, @Query('stateId') stateId?: string) {
    const id = stateId ? parseInt(stateId, 10) : undefined;
    if (id != null && !Number.isNaN(id)) return this.svc.listCities(id, undefined);
    return this.svc.listCities(undefined, stateCode);
  }

  /** Legacy load board parity: shared city/state rows (substring match on city, min length 1). */
  @Get('places')
  places(@Query('q') q = '', @Query('take') take?: string) {
    const n = take != null ? parseInt(take, 10) : undefined;
    return this.svc.searchPlaces(q, n);
  }
}
