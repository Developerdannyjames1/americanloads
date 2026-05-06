import { Column, Entity, PrimaryGeneratedColumn } from 'typeorm';

@Entity({ name: 'Companies' })
export class Company {
  @PrimaryGeneratedColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'Name', type: 'nvarchar', length: 200, nullable: true })
  Name?: string | null;

  @Column({ name: 'CompanyType', type: 'nvarchar', length: 32 })
  CompanyType!: string;

  @Column({ name: 'OnboardingStatus', type: 'nvarchar', length: 32, nullable: true })
  OnboardingStatus?: string | null;

  @Column({ name: 'CreatedUtc', type: 'datetime2' })
  CreatedUtc!: Date;
}
