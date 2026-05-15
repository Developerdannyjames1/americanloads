import * as crypto from 'crypto';

/**
 * ASP.NET Identity v2 password hash format (used by MVC 5 / Owin).
 *
 *   bytes = base64decode(PasswordHash)
 *   bytes[0]      = version marker (0x00)
 *   bytes[1..16]  = 16-byte salt
 *   bytes[17..48] = 32-byte PBKDF2-HMAC-SHA1 output (1000 iterations)
 *
 * Length: 49 bytes total => 68-char base64 string.
 *
 * ASP.NET Identity v3 (used by Core) format:
 *   bytes[0]    = 0x01 version
 *   bytes[1..4] = uint32 BE PRF (0=SHA1, 1=SHA256, 2=SHA512)
 *   bytes[5..8] = uint32 BE iteration count
 *   bytes[9..12]= uint32 BE salt length (N)
 *   bytes[13..13+N-1] = salt
 *   bytes[13+N..]     = subkey
 *
 * Both are supported here.
 */
export function verifyAspNetPassword(stored: string, providedPassword: string): boolean {
  if (!stored || !providedPassword) return false;
  let bytes: Buffer;
  try {
    bytes = Buffer.from(stored, 'base64');
  } catch {
    return false;
  }
  if (bytes.length < 1) return false;
  const version = bytes[0];
  if (version === 0x00) return verifyV2(bytes, providedPassword);
  if (version === 0x01) return verifyV3(bytes, providedPassword);
  return false;
}

function verifyV2(bytes: Buffer, password: string): boolean {
  if (bytes.length !== 1 + 16 + 32) return false;
  const salt = bytes.subarray(1, 17);
  const expected = bytes.subarray(17);
  const derived = crypto.pbkdf2Sync(Buffer.from(password, 'utf8'), salt, 1000, 32, 'sha1');
  return crypto.timingSafeEqual(derived, expected);
}

function verifyV3(bytes: Buffer, password: string): boolean {
  if (bytes.length < 13) return false;
  const prf = bytes.readUInt32BE(1);
  const iter = bytes.readUInt32BE(5);
  const saltLen = bytes.readUInt32BE(9);
  if (bytes.length < 13 + saltLen) return false;
  const salt = bytes.subarray(13, 13 + saltLen);
  const expected = bytes.subarray(13 + saltLen);
  const algo = prf === 0 ? 'sha1' : prf === 1 ? 'sha256' : prf === 2 ? 'sha512' : null;
  if (!algo) return false;
  const derived = crypto.pbkdf2Sync(Buffer.from(password, 'utf8'), salt, iter, expected.length, algo);
  return crypto.timingSafeEqual(derived, expected);
}

/**
 * Hash a new password using ASP.NET Identity v2 layout so the .NET app and
 * the new NestJS app both can verify it.
 */
export function hashAspNetPasswordV2(password: string): string {
  const salt = crypto.randomBytes(16);
  const subkey = crypto.pbkdf2Sync(Buffer.from(password, 'utf8'), salt, 1000, 32, 'sha1');
  const buf = Buffer.concat([Buffer.from([0x00]), salt, subkey]);
  return buf.toString('base64');
}
