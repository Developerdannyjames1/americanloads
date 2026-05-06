import { Column, Entity, PrimaryColumn } from 'typeorm';

@Entity({ name: 'AspNetRoles' })
export class AspNetRole {
  @PrimaryColumn({ name: 'Id', type: 'nvarchar', length: 128 })
  Id!: string;

  @Column({ name: 'Name', type: 'nvarchar', length: 256 })
  Name!: string;
}
