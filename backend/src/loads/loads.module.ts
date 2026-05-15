import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import {
  AspNetUser,
  Company,
  Load,
  LoadType,
  OriginDestination,
  State,
} from '../entities';
import { LoadsService } from './loads.service';
import { LoadsController } from './loads.controller';

@Module({
  imports: [
    TypeOrmModule.forFeature([Load, OriginDestination, State, LoadType, Company, AspNetUser]),
  ],
  providers: [LoadsService],
  controllers: [LoadsController],
  exports: [LoadsService, TypeOrmModule],
})
export class LoadsModule {}
