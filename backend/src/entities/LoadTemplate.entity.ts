import { Column, Entity, JoinColumn, ManyToOne, PrimaryGeneratedColumn } from 'typeorm';
import { OriginDestination } from './OriginDestination.entity';
import { LoadType } from './LoadType.entity';
import { Company } from './Company.entity';

@Entity({ name: 'LoadTemplates' })
export class LoadTemplate {
  @PrimaryGeneratedColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'Name', type: 'nvarchar', length: 120 })
  Name!: string;

  @Column({ name: 'IsGlobal', type: 'bit' })
  IsGlobal!: boolean;

  @Column({ name: 'CompanyId', type: 'int', nullable: true })
  CompanyId?: number | null;

  @Column({ name: 'LoadTypeId', type: 'int', nullable: true })
  LoadTypeId?: number | null;

  @Column({ name: 'AssetLength', type: 'int', nullable: true })
  AssetLength?: number | null;

  @Column({ name: 'Weight', type: 'int', nullable: true })
  Weight?: number | null;

  @Column({ name: 'OriginId', type: 'int', nullable: true })
  OriginId?: number | null;

  @Column({ name: 'DestinationId', type: 'int', nullable: true })
  DestinationId?: number | null;

  @Column({ name: 'OriginCity', type: 'nvarchar', length: 200, nullable: true })
  OriginCity?: string | null;

  @Column({ name: 'OriginState', type: 'nvarchar', length: 10, nullable: true })
  OriginState?: string | null;

  @Column({ name: 'DestinationCity', type: 'nvarchar', length: 200, nullable: true })
  DestinationCity?: string | null;

  @Column({ name: 'DestinationState', type: 'nvarchar', length: 10, nullable: true })
  DestinationState?: string | null;

  @Column({ name: 'Notes', type: 'nvarchar', length: 1000, nullable: true })
  Notes?: string | null;

  @Column({ name: 'CreatedByUserId', type: 'nvarchar', length: 128, nullable: true })
  CreatedByUserId?: string | null;

  @ManyToOne(() => Company, { nullable: true })
  @JoinColumn({ name: 'CompanyId', referencedColumnName: 'Id' })
  Company?: Company;

  @ManyToOne(() => LoadType, { nullable: true })
  @JoinColumn({ name: 'LoadTypeId', referencedColumnName: 'Id' })
  LoadType?: LoadType;

  @ManyToOne(() => OriginDestination, { nullable: true })
  @JoinColumn({ name: 'OriginId', referencedColumnName: 'Id' })
  Origin?: OriginDestination;

  @ManyToOne(() => OriginDestination, { nullable: true })
  @JoinColumn({ name: 'DestinationId', referencedColumnName: 'Id' })
  Destination?: OriginDestination;
}
