import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AspNetRole, AspNetUser, AspNetUserRole, Company, Load, LoadClaim } from '../entities';
import { ClaimsService } from './claims.service';
import { ClaimsController } from './claims.controller';
import { LoadsModule } from '../loads/loads.module';

@Module({
  imports: [
    TypeOrmModule.forFeature([LoadClaim, Load, AspNetUser, Company, AspNetRole, AspNetUserRole]),
    LoadsModule,
  ],
  providers: [ClaimsService],
  controllers: [ClaimsController],
})
export class ClaimsModule {}
