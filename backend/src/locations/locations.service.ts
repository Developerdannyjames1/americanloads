import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { OriginDestination, SitesLocation, State } from '../entities';

type OdPlaceSuggestion = { id: number; city: string; stateCode: string };

/** Escape user text for MSSQL LIKE with literals %, _, [ */
function mssqlContainsPattern(upperQuery: string): string {
  const esc = upperQuery.replace(/\[/g, '[[]').replace(/%/g, '[%]').replace(/_/g, '[_]');
  return `%${esc}%`;
}

@Injectable()
export class LocationsService {
  constructor(
    @InjectRepository(State) private readonly states: Repository<State>,
    @InjectRepository(OriginDestination) private readonly ods: Repository<OriginDestination>,
    @InjectRepository(SitesLocation) private readonly sites: Repository<SitesLocation>,
  ) {}

  /**
   * Office / warehouse sites from dbo.Locations — same source as legacy Users Create/Edit
   * (AspNetUsers.Location stores Locations.Location).
   */
  async listWorkSites() {
    const rows = await this.sites
      .createQueryBuilder('l')
      .where('l.Location IS NOT NULL')
      .andWhere("LTRIM(RTRIM(l.Location)) <> ''")
      .orderBy('l.Location', 'ASC')
      .getMany();
    return rows
      .map((r) => ({
        id: r.Id,
        location: (r.Location || '').trim(),
      }))
      .filter((r) => r.location.length > 0);
  }

  async listStates() {
    const rows = await this.states
      .createQueryBuilder('s')
      .where("s.Code IS NOT NULL AND LTRIM(RTRIM(s.Code)) <> ''")
      .orderBy('s.Code', 'ASC')
      .getMany();
    return rows.map((s) => ({
      id: s.Id,
      code: (s.Code || '').trim().toUpperCase(),
      name: (s.Name || '').trim(),
    }));
  }

  /** Distinct cities for a state (all OD types). Legacy helper; load board autocomplete uses {@link searchPlaces}. */
  async listCities(stateId?: number, stateCode?: string) {
    let sid = stateId;
    if (!sid && stateCode) {
      const code = stateCode.trim().toUpperCase();
      const st = await this.states.findOne({ where: { Code: code } });
      sid = st?.Id;
    }
    if (sid == null) return [];

    const raw = await this.ods
      .createQueryBuilder('od')
      .select('MIN(od.Id)', 'id')
      .addSelect('od.City', 'city')
      .where('od.StateId = :sid', { sid })
      .andWhere('od.City IS NOT NULL')
      .andWhere("LTRIM(RTRIM(od.City)) <> ''")
      .groupBy('od.City')
      .orderBy('od.City', 'ASC')
      .getRawMany<{ city: string }>();

    return raw.map((r) => ({ city: (r.city || '').trim() })).filter((r) => r.city.length > 0);
  }

  /**
   * Same autocomplete source for origin & destination as legacy MVC load board:
   * distinct (city + state code) rows from OriginDestination × States, substring match on city.
   */
  async searchPlaces(qRaw: string, takeOpt?: number): Promise<OdPlaceSuggestion[]> {
    const q = (qRaw || '').trim().toUpperCase();
    if (q.length < 1) return [];

    let take = typeof takeOpt === 'number' && !Number.isNaN(takeOpt) ? Math.floor(takeOpt) : 80;
    take = Math.min(Math.max(take, 1), 200);

    const pat = mssqlContainsPattern(q);

    const raw = await this.ods
      .createQueryBuilder('od')
      .innerJoin('od.State', 'st')
      .select('MIN(od.Id)', 'minId')
      .addSelect('od.City', 'city')
      .addSelect('st.Code', 'stateCode')
      .where('od.City IS NOT NULL')
      .andWhere("LTRIM(RTRIM(od.City)) <> ''")
      .andWhere('st.Code IS NOT NULL')
      .andWhere("LTRIM(RTRIM(st.Code)) <> ''")
      .andWhere('UPPER(LTRIM(RTRIM(od.City))) LIKE :pat', { pat })
      .groupBy('od.City')
      .addGroupBy('st.Code')
      .orderBy('od.City', 'ASC')
      .addOrderBy('st.Code', 'ASC')
      .take(take)
      .getRawMany<{ minId?: number | null; city?: string | null; stateCode?: string | null }>();

    return raw
      .map((r) => ({
        id: Number(r.minId || 0),
        city: (r.city || '').trim(),
        stateCode: (r.stateCode || '').trim().toUpperCase(),
      }))
      .filter((r) => r.id > 0 && r.city.length > 0 && r.stateCode.length > 0);
  }
}
