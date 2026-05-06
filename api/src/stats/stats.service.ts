import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Company, Load, LoadClaim } from '../entities';
import { ClaimStatus, LoadStatus, OnboardingStatus, Roles } from '../common/constants';

type Caller = { sub: string; role: string; companyId: number | null };

@Injectable()
export class StatsService {
  constructor(
    @InjectRepository(Load) private readonly loads: Repository<Load>,
    @InjectRepository(LoadClaim) private readonly claims: Repository<LoadClaim>,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
  ) {}

  async kpis(caller: Caller) {
    const isAdmin = caller.role === Roles.Admin;
    const qb = this.loads.createQueryBuilder('l');
    if (caller.role === Roles.Shipper) {
      qb.where('(l.ShipperUserId = :uid OR l.CompanyId = :cid)', {
        uid: caller.sub,
        cid: caller.companyId ?? -1,
      });
    } else if (caller.role === Roles.Carrier) {
      qb.where('l.AssignedCarrierUserId = :uid', { uid: caller.sub });
    } else if (caller.role === Roles.Dispatcher) {
      qb.where('(l.CompanyId = :cid OR l.AssignedCarrierUserId IN (SELECT Id FROM AspNetUsers WHERE CompanyId = :cid))', {
        cid: caller.companyId ?? -1,
      });
    }
    const all = await qb.getMany();

    const counts: Record<string, number> = {};
    let revenue = 0;
    let cost = 0;
    let openCount = 0;
    let activeCount = 0;
    let completedCount = 0;
    for (const l of all) {
      const st = (l.WorkflowStatus || LoadStatus.Posted).toLowerCase();
      counts[st] = (counts[st] || 0) + 1;
      const billed = l.CustomerAmount != null ? Number(l.CustomerAmount) : 0;
      const paid = l.CarrierAmount != null ? Number(l.CarrierAmount) : 0;
      revenue += billed;
      cost += paid;
      if (st === LoadStatus.Posted || st === LoadStatus.Draft) openCount++;
      if (st === LoadStatus.Assigned || st === LoadStatus.InTransit) activeCount++;
      if (st === LoadStatus.Completed || st === LoadStatus.Delivered) completedCount++;
    }
    const profit = revenue - cost;
    const margin = revenue > 0 ? (profit / revenue) * 100 : 0;

    let pendingClaims = 0;
    if (isAdmin) {
      pendingClaims = await this.claims.count({ where: { Status: ClaimStatus.Pending } });
    } else if (caller.role === Roles.Carrier) {
      pendingClaims = await this.claims.count({
        where: { Status: ClaimStatus.Pending, CarrierUserId: caller.sub },
      });
    }

    let pendingCompanies = 0;
    if (isAdmin) {
      pendingCompanies = await this.companies.count({
        where: { OnboardingStatus: OnboardingStatus.Pending },
      });
    }

    return {
      totals: {
        loads: all.length,
        revenue: Number(revenue.toFixed(2)),
        cost: Number(cost.toFixed(2)),
        profit: Number(profit.toFixed(2)),
        marginPercent: Number(margin.toFixed(2)),
      },
      open: openCount,
      active: activeCount,
      completed: completedCount,
      pendingClaims,
      pendingCompanies,
      byStatus: counts,
      donut: [
        { label: 'Cost', value: Number(cost.toFixed(2)) },
        { label: 'Profit', value: Number(Math.max(profit, 0).toFixed(2)) },
      ],
    };
  }
}
