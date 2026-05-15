import { Column, Entity, JoinColumn, ManyToOne, PrimaryGeneratedColumn } from 'typeorm';
import { State } from './State.entity';

@Entity({ name: 'OriginDestination' })
export class OriginDestination {
  @PrimaryGeneratedColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'Type', type: 'smallint' })
  Type!: number;

  @Column({ name: 'City', type: 'nvarchar', length: 'MAX', nullable: true })
  City?: string | null;

  @Column({ name: 'County', type: 'nvarchar', length: 'MAX', nullable: true })
  County?: string | null;

  @Column({ name: 'StateId', type: 'int' })
  StateId!: number;

  @Column({ name: 'PostalCode', type: 'nvarchar', length: 50, nullable: true })
  PostalCode?: string | null;

  @Column({ name: 'Country', type: 'nvarchar', length: 100, nullable: true })
  Country?: string | null;

  @Column({ name: 'Latitude', type: 'decimal', precision: 18, scale: 6, nullable: true })
  Latitude?: number | null;

  @Column({ name: 'Longitude', type: 'decimal', precision: 18, scale: 6, nullable: true })
  Longitude?: number | null;

  @ManyToOne(() => State)
  @JoinColumn({ name: 'StateId', referencedColumnName: 'Id' })
  State?: State;
}
