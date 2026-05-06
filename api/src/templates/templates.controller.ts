import { Body, Controller, Delete, Get, Param, Post, UseGuards } from '@nestjs/common';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { CurrentUser } from '../common/decorators/current-user.decorator';
import { TemplatesService } from './templates.service';
import { SaveTemplateDto } from './dto';

@UseGuards(JwtAuthGuard)
@Controller('templates')
export class TemplatesController {
  constructor(private readonly svc: TemplatesService) {}

  @Get()
  list(@CurrentUser() user: any) {
    return this.svc.list(user);
  }

  @Post()
  save(@CurrentUser() user: any, @Body() dto: SaveTemplateDto) {
    return this.svc.save(user, dto);
  }

  @Delete(':id')
  remove(@CurrentUser() user: any, @Param('id') id: string) {
    return this.svc.remove(user, parseInt(id, 10));
  }
}
