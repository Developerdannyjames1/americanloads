import { Column, Entity, PrimaryGeneratedColumn } from 'typeorm';

@Entity({ name: 'LoadClaims' })
export class LoadClaim {
  @PrimaryGeneratedColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'LoadId', type: 'int' })
  LoadId!: number;

  @Column({ name: 'CarrierUserId', type: 'nvarchar', length: 128 })
  CarrierUserId!: string;

  @Column({ name: 'ClaimType', type: 'nvarchar', length: 16 })
  ClaimType!: string;

  @Column({ name: 'BidAmount', type: 'decimal', precision: 18, scale: 2, nullable: true })
  BidAmount?: number | null;

  @Column({ name: 'Message', type: 'nvarchar', length: 2000, nullable: true })
  Message?: string | null;

  @Column({ name: 'Status', type: 'nvarchar', length: 32 })
  Status!: string;

  @Column({ name: 'CreatedUtc', type: 'datetime2' })
  CreatedUtc!: Date;

  @Column({ name: 'ResolvedUtc', type: 'datetime2', nullable: true })
  ResolvedUtc?: Date | null;

  @Column({ name: 'ResolvedByUserId', type: 'nvarchar', length: 128, nullable: true })
  ResolvedByUserId?: string | null;
}
