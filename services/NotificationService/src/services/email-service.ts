import nodemailer from 'nodemailer';
import { config } from '../config';
import { createLogger } from '@hospital/shared';

const logger = createLogger('notification-service');

export class EmailService {
  private readonly transporter: nodemailer.Transporter;

  constructor() {
    this.transporter = nodemailer.createTransport({
      host: config.email.host,
      port: config.email.port,
      auth: config.email.user
        ? { user: config.email.user, pass: config.email.password }
        : undefined,
    });
  }

  async send(to: string, subject: string, body: string): Promise<void> {
    try {
      await this.transporter.sendMail({ from: config.email.from, to, subject, html: body });
      logger.info('Email sent', { to, subject });
    } catch (err) {
      logger.error('Email send failed', { to, error: err });
      // Don't throw — notifications are best-effort in scaffold
    }
  }
}
