import { Body, Controller, Delete, Get, Param, Patch, Post, UseGuards } from '@nestjs/common';
import { JwtAuthGuard } from '../common/guards/jwt-auth.guard';
import { RolesGuard } from '../common/guards/roles.guard';
import { RequireRoles } from '../common/decorators/roles.decorator';
import { CurrentUser } from '../common/decorators/current-user.decorator';
import { Roles } from '../common/constants';
import { UsersService } from './users.service';
import { UpdateUserDto } from './dto/update-user.dto';
import { CreateUserDto } from './dto/create-user.dto';

@UseGuards(JwtAuthGuard)
@Controller('users')
export class UsersController {
  constructor(private readonly users: UsersService) {}

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Get()
  list() {
    return this.users.list();
  }

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Post()
  create(@Body() dto: CreateUserDto) {
    return this.users.create(dto);
  }

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Patch(':id/active/:active')
  setActive(@Param('id') id: string, @Param('active') active: string) {
    return this.users.setActive(id, active === 'true');
  }

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Patch(':id')
  update(@Param('id') id: string, @Body() dto: UpdateUserDto) {
    return this.users.update(id, dto);
  }

  @UseGuards(RolesGuard)
  @RequireRoles(Roles.Admin)
  @Delete(':id')
  remove(@CurrentUser() actor: any, @Param('id') id: string) {
    return this.users.remove(actor?.sub ?? '', id);
  }
}
