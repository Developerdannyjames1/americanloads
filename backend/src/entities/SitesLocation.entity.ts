import { Column, Entity, PrimaryGeneratedColumn } from 'typeorm';

/** Legacy dbo.Locations — office / site names stored on AspNetUsers.Location (varchar 15). */
@Entity({ name: 'Locations' })
export class SitesLocation {
  @PrimaryGeneratedColumn({ name: 'Id', type: 'int' })
  Id!: number;

  @Column({ name: 'Location', type: 'varchar', length: 15, nullable: true })
  Location?: string | null;
}
