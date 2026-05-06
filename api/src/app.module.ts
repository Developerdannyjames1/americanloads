import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import {
  AspNetRole,
  AspNetUser,
  AspNetUserRole,
  Company,
  Load,
  LoadClaim,
  LoadTemplate,
  LoadType,
  OriginDestination,
  SitesLocation,
  State,
} from './entities';
import { AuthModule } from './auth/auth.module';
import { UsersModule } from './users/users.module';
import { CompaniesModule } from './companies/companies.module';
import { LoadsModule } from './loads/loads.module';
import { TemplatesModule } from './templates/templates.module';
import { ClaimsModule } from './claims/claims.module';
import { RealtimeModule } from './realtime/realtime.module';
import { MailModule } from './mail/mail.module';
import { StatsModule } from './stats/stats.module';
import { LocationsModule } from './locations/locations.module';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    TypeOrmModule.forRootAsync({
      inject: [ConfigService],
      useFactory: (cfg: ConfigService) => ({
        type: 'mssql',
        host: cfg.get<string>('DB_HOST') || 'localhost',
        port: cfg.get<string>('DB_INSTANCE')
          ? undefined
          : parseInt(cfg.get<string>('DB_PORT') || '1433', 10),
        username: cfg.get<string>('DB_USER') || 'sa',
        password: cfg.get<string>('DB_PASS') || '',
        database: cfg.get<string>('DB_NAME') || 'Ast',
        entities: [
          AspNetUser,
          AspNetRole,
          AspNetUserRole,
          Company,
          Load,
          LoadTemplate,
          LoadClaim,
          OriginDestination,
          State,
          SitesLocation,
          LoadType,
        ],
        synchronize: false,
        logging: false,
        retryAttempts: 3,
        retryDelay: 1000,
        options: {
          instanceName: cfg.get<string>('DB_INSTANCE') || undefined,
          encrypt: cfg.get<string>('DB_ENCRYPT') === 'true',
          trustServerCertificate: cfg.get<string>('DB_TRUST_SERVER_CERT') !== 'false',
        },
      }),
    }),
    MailModule,
    AuthModule,
    UsersModule,
    CompaniesModule,
    LoadsModule,
    TemplatesModule,
    ClaimsModule,
    RealtimeModule,
    StatsModule,
    LocationsModule,
  ],
})
export class AppModule {}
