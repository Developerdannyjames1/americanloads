import { BadRequestException, ForbiddenException, Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Company, LoadTemplate, LoadType } from '../entities';
import { Roles } from '../common/constants';
import { SaveTemplateDto } from './dto';
import { packTemplateNotes, unpackTemplateNotes } from './template-notes';

type Caller = { sub: string; role: string; companyId: number | null };

import { unpackTemplateNotes } from './template-notes';

function resolveTemplateText(dto: SaveTemplateDto): {
  description: string | null;
  userNotes: string | null;
} {
  let description: string | null = null;
  let userNotes: string | null = null;
  if (dto.description !== undefined || dto.userNotes !== undefined) {
    if (dto.description !== undefined) description = (dto.description || '').trim() || null;
    if (dto.userNotes !== undefined) userNotes = (dto.userNotes || '').trim() || null;
    if (!description && !userNotes && dto.notes !== undefined) {
      const unpacked = unpackTemplateNotes(dto.notes);
      description = unpacked.description || null;
      userNotes = unpacked.userNotes || null;
    }
  } else if (dto.notes !== undefined) {
    const unpacked = unpackTemplateNotes(dto.notes);
    description = unpacked.description || null;
    userNotes = unpacked.userNotes || null;
  }
  return { description, userNotes };
}

@Injectable()
export class TemplatesService {
  constructor(
    @InjectRepository(LoadTemplate) private readonly tmpls: Repository<LoadTemplate>,
    @InjectRepository(LoadType) private readonly loadTypes: Repository<LoadType>,
    @InjectRepository(Company) private readonly companies: Repository<Company>,
  ) {}

  shape(t: LoadTemplate) {
    const { description, userNotes } = unpackTemplateNotes(t.Notes);
    return {
      id: t.Id,
      _id: String(t.Id),
      name: t.Name,
      isGlobal: !!t.IsGlobal,
      companyId: t.CompanyId ?? null,
      loadTypeId: t.LoadTypeId ?? null,
      assetLength: t.AssetLength ?? null,
      weight: t.Weight ?? null,
      origin: t.OriginCity || t.OriginState
        ? { city: t.OriginCity || '', state: t.OriginState || '', zip: '' }
        : { city: '', state: '', zip: '' },
      destination: t.DestinationCity || t.DestinationState
        ? { city: t.DestinationCity || '', state: t.DestinationState || '', zip: '' }
        : { city: '', state: '', zip: '' },
      description,
      userNotes,
      notes: userNotes || description,
    };
  }

  async list(caller: Caller) {
    const isAdmin = caller.role === Roles.Admin;
    let rows: LoadTemplate[];
    if (isAdmin) {
      rows = await this.tmpls.find({ order: { Name: 'ASC' } });
    } else if (caller.companyId != null) {
      rows = await this.tmpls
        .createQueryBuilder('t')
        .where('t.IsGlobal = 1')
        .orWhere('t.CompanyId = :cid', { cid: caller.companyId })
        .orderBy('t.Name', 'ASC')
        .getMany();
    } else {
      rows = await this.tmpls.find({ where: { IsGlobal: true }, order: { Name: 'ASC' } });
    }
    return rows.map((t) => this.shape(t));
  }

  async save(caller: Caller, dto: SaveTemplateDto) {
    const isAdmin = caller.role === Roles.Admin;
    if (!isAdmin && caller.role !== Roles.Shipper) throw new ForbiddenException();
    if (!dto.name || dto.name.trim().length < 2) throw new BadRequestException('Name is required');

    const isGlobal = !!dto.isGlobal;
    let companyId: number | null = null;
    if (!isGlobal) {
      if (isAdmin) {
        if (!dto.companyId) throw new BadRequestException('Company template requires a company');
        const co = await this.companies.findOne({ where: { Id: dto.companyId } });
        if (!co) throw new BadRequestException('Company not found');
        companyId = co.Id;
      } else {
        if (caller.companyId == null) throw new BadRequestException('No company on user');
        companyId = caller.companyId;
      }
    }

    let entity: LoadTemplate;
    if (dto.id) {
      const existing = await this.tmpls.findOne({ where: { Id: dto.id } });
      if (!existing) throw new NotFoundException();
      if (!isAdmin && existing.CompanyId !== caller.companyId) throw new ForbiddenException();
      entity = existing;
    } else {
      entity = this.tmpls.create({ CreatedByUserId: caller.sub });
    }
    entity.Name = dto.name.trim();
    entity.IsGlobal = isGlobal;
    entity.CompanyId = isGlobal ? null : companyId;
    entity.LoadTypeId = dto.loadTypeId ?? null;
    entity.AssetLength = dto.assetLength ?? null;
    entity.Weight = dto.weight ?? null;
    entity.OriginCity = dto.origin?.city || null;
    entity.OriginState = dto.origin?.state || null;
    entity.DestinationCity = dto.destination?.city || null;
    entity.DestinationState = dto.destination?.state || null;
    const { description, userNotes } = resolveTemplateText(dto);
    entity.Notes = packTemplateNotes(description, userNotes);

    const saved = await this.tmpls.save(entity);
    return this.shape(saved);
  }

  async remove(caller: Caller, id: number) {
    const t = await this.tmpls.findOne({ where: { Id: id } });
    if (!t) throw new NotFoundException();
    const isAdmin = caller.role === Roles.Admin;
    if (!isAdmin) {
      if (caller.role !== Roles.Shipper) throw new ForbiddenException();
      if (t.IsGlobal) throw new ForbiddenException();
      if (caller.companyId == null || t.CompanyId !== caller.companyId) throw new ForbiddenException();
    }
    await this.tmpls.remove(t);
    return { ok: true };
  }
}
