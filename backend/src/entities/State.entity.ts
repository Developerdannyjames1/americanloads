import { Column, Entity, PrimaryColumn } from 'typeorm';

@Entity({ name: 'States' })
export class State {
  @PrimaryColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'Code', type: 'nvarchar', length: 2, nullable: true })
  Code?: string | null;

  @Column({ name: 'Name', type: 'nvarchar', length: 50, nullable: true })
  Name?: string | null;

  @Column({ name: 'Country', type: 'nvarchar', length: 100, nullable: true })
  Country?: string | null;
}
