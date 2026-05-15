import { Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Company } from '../entities';
import { CompanyType, OnboardingStatus } from '../common/constants';

@Injectable()
export class CompaniesService {
  constructor(@InjectRepository(Company) private readonly companies: Repository<Company>) {}

  async list(filter: { type?: string; status?: string; search?: string } = {}) {
    const qb = this.companies.createQueryBuilder('c').orderBy('c.Name', 'ASC');
    if (filter.type) {
      const v = filter.type.toLowerCase() === 'shipper' ? CompanyType.Shipper : CompanyType.Carrier;
      qb.andWhere('LOWER(c.CompanyType) = LOWER(:t)', { t: v });
    }
    if (filter.status) {
      qb.andWhere('LOWER(c.OnboardingStatus) = LOWER(:s)', { s: filter.status });
    }
    const raw = (filter.search || '').trim();
    if (raw.length > 0) {
      const esc = raw.replace(/\[/g, '[[]').replace(/%/g, '[%]').replace(/_/g, '[_]');
      qb.andWhere('LOWER(c.Name) LIKE LOWER(:namePat)', { namePat: `%${esc}%` });
    }
    const rows = await qb.getMany();
    return rows.map(this.shape);
  }

  shape(c: Company) {
    return {
      id: c.Id,
      name: c.Name,
      companyType: c.CompanyType,
      onboardingStatus: c.OnboardingStatus,
      createdAt: c.CreatedUtc,
    };
  }

  async byId(id: number) {
    const c = await this.companies.findOne({ where: { Id: id } });
    if (!c) return null;
    return this.shape(c);
  }

  async setStatus(id: number, status: string) {
    const allowed: string[] = Object.values(OnboardingStatus);
    if (!allowed.includes(status)) throw new Error('Invalid status');
    const c = await this.companies.findOne({ where: { Id: id } });
    if (!c) throw new NotFoundException();
    c.OnboardingStatus = status;
    await this.companies.save(c);
    return this.shape(c);
  }
}
