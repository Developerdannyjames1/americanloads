import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import * as nodemailer from 'nodemailer';

@Injectable()
export class MailService {
  private readonly logger = new Logger(MailService.name);
  private transporter: nodemailer.Transporter | null = null;

  constructor(private readonly cfg: ConfigService) {
    this.init();
  }

  private init() {
    const host = this.cfg.get<string>('SMTP_HOST');
    const port = parseInt(this.cfg.get<string>('SMTP_PORT') || '587', 10);
    const user = this.cfg.get<string>('SMTP_USER');
    const pass = this.cfg.get<string>('SMTP_PASS');
    const useSsl = this.cfg.get<string>('SMTP_USE_SSL') !== 'false';
    if (!host) {
      this.logger.warn('SMTP_HOST not set; emails will only log to console');
      return;
    }
    this.transporter = nodemailer.createTransport({
      host,
      port,
      secure: port === 465,
      auth: user ? { user, pass } : undefined,
      requireTLS: useSsl && port !== 465,
    });
  }

  async send(to: string, subject: string, html: string): Promise<void> {
    const from = this.cfg.get<string>('SMTP_FROM') || 'no-reply@americanloads.local';
    if (!this.transporter) {
      this.logger.log(`[MAIL stub] to=${to} subject=${subject}`);
      this.logger.debug(html);
      return;
    }
    try {
      await this.transporter.sendMail({ from, to, subject, html });
      this.logger.log(`Sent mail to ${to}: ${subject}`);
    } catch (err: any) {
      this.logger.error(`SMTP send failed: ${err?.message || err}`);
      throw err;
    }
  }
}
