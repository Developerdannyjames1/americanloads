import './bootstrap-driver';
import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { AppModule } from './app.module';
import cookieParser from 'cookie-parser';

async function bootstrap() {
  const app = await NestFactory.create(AppModule, { cors: false });
  const port = parseInt(process.env.PORT || '4000', 10);
  const webOrigin = process.env.WEB_ORIGIN || 'http://localhost:3000';

  app.use(cookieParser());
  app.enableCors({
    origin: webOrigin,
    credentials: true,
    methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
  });
  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      transform: true,
      forbidNonWhitelisted: false,
    }),
  );
  app.setGlobalPrefix('api');

  await app.listen(port);
  console.log(`[americanloads-api] listening on http://localhost:${port}/api`);
}

bootstrap();
