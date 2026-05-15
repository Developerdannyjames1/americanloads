import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { Company, LoadTemplate, LoadType } from '../entities';
import { TemplatesService } from './templates.service';
import { TemplatesController } from './templates.controller';

@Module({
  imports: [TypeOrmModule.forFeature([LoadTemplate, LoadType, Company])],
  providers: [TemplatesService],
  controllers: [TemplatesController],
  exports: [TemplatesService, TypeOrmModule],
})
export class TemplatesModule {}
