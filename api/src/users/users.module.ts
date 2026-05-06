import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AspNetRole, AspNetUser, AspNetUserRole, Company, Load, LoadClaim } from '../entities';
import { UsersService } from './users.service';
import { UsersController } from './users.controller';

@Module({
  imports: [
    TypeOrmModule.forFeature([
      AspNetUser,
      AspNetRole,
      AspNetUserRole,
      Company,
      Load,
      LoadClaim,
    ]),
  ],
  providers: [UsersService],
  controllers: [UsersController],
  exports: [UsersService, TypeOrmModule],
})
export class UsersModule {}
