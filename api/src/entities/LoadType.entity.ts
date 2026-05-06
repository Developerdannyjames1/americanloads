import { Column, Entity, PrimaryColumn } from 'typeorm';

@Entity({ name: 'LoadTypes' })
export class LoadType {
  @PrimaryColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'Name', type: 'nvarchar', length: 200 })
  Name!: string;

  @Column({ name: 'IdDAT', type: 'nvarchar', length: 200 })
  IdDAT!: string;

  @Column({ name: 'NameDAT', type: 'nvarchar', length: 200 })
  NameDAT!: string;

  @Column({ name: 'IdTS', type: 'nvarchar', length: 200 })
  IdTS!: string;

  @Column({ name: 'NameTS', type: 'nvarchar', length: 200 })
  NameTS!: string;

  @Column({ name: 'TsId', type: 'int', nullable: true })
  TsId?: number | null;
}
