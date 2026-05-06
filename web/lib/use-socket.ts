'use client';
import { useEffect, useRef } from 'react';
import { io, Socket } from 'socket.io-client';
import { useUser } from './user-context';

export function useSocket(handler: (event: string, payload: any) => void) {
  const session = useUser();
  const socketRef = useRef<Socket | null>(null);

  useEffect(() => {
    if (!session) return;
    const url = process.env.NEXT_PUBLIC_SOCKET_URL || 'http://localhost:4000';
    const s = io(url, {
      auth: {
        userId: session.user.sub,
        role: session.user.role,
      },
      withCredentials: true,
      transports: ['websocket', 'polling'],
    });
    socketRef.current = s;

    const events = [
      'new_claim',
      'new_bid',
      'claim_updated',
      'claim_accepted',
      'claim_rejected',
      'load_updated',
      'tracking_update',
      'load_assigned',
    ];
    events.forEach((ev) => s.on(ev, (p: any) => handler(ev, p)));

    return () => {
      s.disconnect();
    };
  }, [session, handler]);
}
