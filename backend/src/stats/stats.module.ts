import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { Company, Load, LoadClaim } from '../entities';
import { StatsService } from './stats.service';
import { StatsController } from './stats.controller';

@Module({
  imports: [TypeOrmModule.forFeature([Load, LoadClaim, Company])],
  providers: [StatsService],
  controllers: [StatsController],
})
export class StatsModule {}
