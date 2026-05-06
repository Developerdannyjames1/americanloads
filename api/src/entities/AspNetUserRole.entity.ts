import { Entity, PrimaryColumn } from 'typeorm';

@Entity({ name: 'AspNetUserRoles' })
export class AspNetUserRole {
  @PrimaryColumn({ name: 'UserId', type: 'nvarchar', length: 128 })
  UserId!: string;

  @PrimaryColumn({ name: 'RoleId', type: 'nvarchar', length: 128 })
  RoleId!: string;
}
