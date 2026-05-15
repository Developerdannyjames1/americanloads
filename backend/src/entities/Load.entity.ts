import { Column, Entity, JoinColumn, ManyToOne, PrimaryGeneratedColumn } from 'typeorm';
import { OriginDestination } from './OriginDestination.entity';
import { LoadType } from './LoadType.entity';
import { Company } from './Company.entity';

@Entity({ name: 'Loads' })
export class Load {
  @PrimaryGeneratedColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'PostersReferenceId', type: 'nvarchar', length: 'MAX', nullable: true })
  PostersReferenceId?: string | null;

  @Column({ name: 'EquipmentType', type: 'nvarchar', length: 'MAX', nullable: true })
  EquipmentType?: string | null;

  @Column({ name: 'OriginId', type: 'int' })
  OriginId!: number;

  @Column({ name: 'DestinationId', type: 'int', nullable: true })
  DestinationId?: number | null;

  @Column({ name: 'LoadTypeId', type: 'int', nullable: true })
  LoadTypeId?: number | null;

  @Column({ name: 'CompanyId', type: 'int', nullable: true })
  CompanyId?: number | null;

  @Column({ name: 'ShipperUserId', type: 'nvarchar', length: 128, nullable: true })
  ShipperUserId?: string | null;

  @Column({ name: 'AssignedCarrierUserId', type: 'nvarchar', length: 128, nullable: true })
  AssignedCarrierUserId?: string | null;

  @Column({ name: 'WorkflowStatus', type: 'nvarchar', length: 32, nullable: true })
  WorkflowStatus?: string | null;

  @Column({ name: 'CarrierAmount', type: 'decimal', precision: 18, scale: 2 })
  CarrierAmount!: number;

  @Column({ name: 'CustomerAmount', type: 'decimal', precision: 18, scale: 2, nullable: true })
  CustomerAmount?: number | null;

  @Column({ name: 'PickUpDate', type: 'datetime', nullable: true })
  PickUpDate?: Date | null;

  @Column({ name: 'DeliveryDate', type: 'datetime', nullable: true })
  DeliveryDate?: Date | null;

  @Column({ name: 'AssetLength', type: 'int', nullable: true })
  AssetLength?: number | null;

  @Column({ name: 'Weight', type: 'int', nullable: true })
  Weight?: number | null;

  @Column({ name: 'Commodity', type: 'nvarchar', length: 500, nullable: true })
  Commodity?: string | null;

  @Column({ name: 'DateLoaded', type: 'datetime', nullable: true })
  DateLoaded?: Date | null;

  @Column({ name: 'UntilDate', type: 'datetime', nullable: true })
  UntilDate?: Date | null;

  @Column({ name: 'Description', type: 'nvarchar', length: 'MAX', nullable: true })
  Description?: string | null;

  @Column({ name: 'UserNotes', type: 'varchar', length: 'MAX', nullable: true })
  UserNotes?: string | null;

  @Column({ name: 'Comments', type: 'varchar', length: 'MAX', nullable: true })
  Comments?: string | null;

  @Column({ name: 'IsLoadFull', type: 'bit' })
  IsLoadFull!: boolean;

  @Column({ name: 'AllowUntilSat', type: 'bit', nullable: true })
  AllowUntilSat?: boolean | null;

  @Column({ name: 'AllowUntilSun', type: 'bit', nullable: true })
  AllowUntilSun?: boolean | null;

  @Column({ name: 'CreateDate', type: 'datetime', nullable: true })
  CreateDate?: Date | null;

  @Column({ name: 'CreatedBy', type: 'nvarchar', length: 256, nullable: true })
  CreatedBy?: string | null;

  @Column({ name: 'UpdateDate', type: 'datetime', nullable: true })
  UpdateDate?: Date | null;

  @Column({ name: 'UpdatedBy', type: 'nvarchar', length: 256, nullable: true })
  UpdatedBy?: string | null;

  @Column({ name: 'AvailabilityEarliest', type: 'datetime', nullable: true })
  AvailabilityEarliest?: Date | null;

  @Column({ name: 'AvailabilityLatest', type: 'datetime', nullable: true })
  AvailabilityLatest?: Date | null;

  @Column({ name: 'RateEateBasedOn', type: 'smallint' })
  RateEateBasedOn!: number;

  @ManyToOne(() => OriginDestination, { nullable: true })
  @JoinColumn({ name: 'OriginId', referencedColumnName: 'Id' })
  Origin?: OriginDestination;

  @ManyToOne(() => OriginDestination, { nullable: true })
  @JoinColumn({ name: 'DestinationId', referencedColumnName: 'Id' })
  Destination?: OriginDestination;

  @ManyToOne(() => LoadType, { nullable: true })
  @JoinColumn({ name: 'LoadTypeId', referencedColumnName: 'Id' })
  LoadType?: LoadType;

  @ManyToOne(() => Company, { nullable: true })
  @JoinColumn({ name: 'CompanyId', referencedColumnName: 'Id' })
  Company?: Company;
}
