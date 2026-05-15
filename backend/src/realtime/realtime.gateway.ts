import { Logger } from '@nestjs/common';
import {
  ConnectedSocket,
  OnGatewayConnection,
  OnGatewayDisconnect,
  WebSocketGateway,
  WebSocketServer,
} from '@nestjs/websockets';
import type { Server, Socket } from 'socket.io';

type EventName =
  | 'new_claim'
  | 'new_bid'
  | 'load_updated'
  | 'tracking_update'
  | 'load_assigned'
  | 'claim_submitted'
  | 'claim_updated'
  | 'claim_accepted'
  | 'claim_rejected';

@WebSocketGateway({
  cors: {
    origin: process.env.WEB_ORIGIN || 'http://localhost:3000',
    credentials: true,
  },
  path: '/socket.io',
})
export class RealtimeGateway implements OnGatewayConnection, OnGatewayDisconnect {
  private readonly logger = new Logger('RealtimeGateway');

  @WebSocketServer()
  server!: Server;

  handleConnection(@ConnectedSocket() client: Socket) {
    const role = (client.handshake.auth?.role as string) || 'guest';
    const userId = (client.handshake.auth?.userId as string) || '';
    if (role === 'admin') client.join('admins');
    if (userId) client.join(`user:${userId}`);
    this.logger.log(`socket connected: ${client.id} role=${role}`);
  }

  handleDisconnect(client: Socket) {
    this.logger.log(`socket disconnected: ${client.id}`);
  }

  emitToAdmins(event: EventName, payload: unknown) {
    this.server.to('admins').emit(event, payload);
  }

  emitToUser(userId: string, event: EventName, payload: unknown) {
    if (!userId) return;
    this.server.to(`user:${userId}`).emit(event, payload);
  }

  broadcast(event: EventName, payload: unknown) {
    this.server.emit(event, payload);
  }
}
