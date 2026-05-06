import {
  Column,
  Entity,
  JoinTable,
  ManyToMany,
  PrimaryColumn,
} from 'typeorm';
import { AspNetRole } from './AspNetRole.entity';

@Entity({ name: 'AspNetUsers' })
export class AspNetUser {
  @PrimaryColumn({ name: 'Id', type: 'nvarchar', length: 128 })
  Id!: string;

  @Column({ name: 'Email', type: 'nvarchar', length: 256, nullable: true })
  Email?: string | null;

  @Column({ name: 'EmailConfirmed', type: 'bit' })
  EmailConfirmed!: boolean;

  @Column({ name: 'PasswordHash', type: 'nvarchar', length: 'MAX', nullable: true })
  PasswordHash?: string | null;

  @Column({ name: 'SecurityStamp', type: 'nvarchar', length: 'MAX', nullable: true })
  SecurityStamp?: string | null;

  @Column({ name: 'PhoneNumber', type: 'nvarchar', length: 'MAX', nullable: true })
  PhoneNumber?: string | null;

  @Column({ name: 'PhoneNumberConfirmed', type: 'bit' })
  PhoneNumberConfirmed!: boolean;

  @Column({ name: 'TwoFactorEnabled', type: 'bit' })
  TwoFactorEnabled!: boolean;

  @Column({ name: 'LockoutEndDateUtc', type: 'datetime', nullable: true })
  LockoutEndDateUtc?: Date | null;

  @Column({ name: 'LockoutEnabled', type: 'bit' })
  LockoutEnabled!: boolean;

  @Column({ name: 'AccessFailedCount', type: 'int' })
  AccessFailedCount!: number;

  @Column({ name: 'UserName', type: 'nvarchar', length: 256 })
  UserName!: string;

  @Column({ name: 'FullName', type: 'varchar', length: 'MAX', nullable: true })
  FullName?: string | null;

  @Column({ name: 'Phone', type: 'varchar', length: 'MAX', nullable: true })
  Phone?: string | null;

  @Column({ name: 'Extension', type: 'varchar', length: 'MAX', nullable: true })
  Extension?: string | null;

  @Column({ name: 'Email2', type: 'varchar', length: 'MAX', nullable: true })
  Email2?: string | null;

  @Column({ name: 'Location', type: 'varchar', length: 15, nullable: true })
  Location?: string | null;

  @Column({ name: 'CarrierApprovalStatus', type: 'nvarchar', length: 32, nullable: true })
  CarrierApprovalStatus?: string | null;

  @Column({ name: 'CompanyId', type: 'int', nullable: true })
  CompanyId?: number | null;

  @ManyToMany(() => AspNetRole)
  @JoinTable({
    name: 'AspNetUserRoles',
    joinColumn: { name: 'UserId', referencedColumnName: 'Id' },
    inverseJoinColumn: { name: 'RoleId', referencedColumnName: 'Id' },
  })
  Roles?: AspNetRole[];
}
