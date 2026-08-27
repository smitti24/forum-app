import { MemberSchema, RoleSchema } from '../auth/member.schema';
import { AuthResponseSchema } from '../../features/auth/data/auth.schema';
import { PostSchema } from '../../features/posts/data/post.schema';

const member = {
  id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e6f',
  username: 'asmith',
  email: 'asmith@example.com',
  role: 'member',
};

describe('the API contract', () => {
  it('accepts an AuthResponse as the API serialises it', () => {
    const parsed = AuthResponseSchema.parse({
      token: 'header.payload.signature',
      expiresAt: '2026-08-27T07:31:46.1234567Z',
      member,
    });

    expect(parsed.member.role).toBe('member');
  });

  it('accepts the ProfileResponse extra fields and strips them', () => {
    const parsed = MemberSchema.parse({ ...member, createdAt: '2026-08-27T06:31:46Z' });

    expect(parsed).not.toHaveProperty('createdAt');
  });

  it('reads roles in the lower case the API emits', () => {
    expect(RoleSchema.parse('moderator')).toBe('moderator');
    expect(RoleSchema.safeParse('Moderator').success).toBe(false);
  });

  it('accepts an unflagged post', () => {
    const parsed = PostSchema.parse({
      id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e70',
      title: 'Integrating the SDK',
      body: 'Body text.',
      author: { id: member.id, username: member.username },
      createdAt: '2026-08-27T06:31:46.1234567Z',
      likeCount: 0,
      commentCount: 0,
      likedByCurrentMember: false,
      flag: null,
    });

    expect(parsed.flag).toBeNull();
  });

  it('accepts a flagged post with its moderator and timestamp', () => {
    const parsed = PostSchema.parse({
      id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e71',
      title: 'Disputed claim',
      body: 'Body text.',
      author: { id: member.id, username: member.username },
      createdAt: '2026-08-27T06:31:46Z',
      likeCount: 3,
      commentCount: 1,
      likedByCurrentMember: true,
      flag: { flaggedBy: 'moderator1', flaggedAt: '2026-08-27T07:00:00Z' },
    });

    expect(parsed.flag?.flaggedBy).toBe('moderator1');
  });

  it('rejects a timestamp with no timezone, which would mean a lost UTC marker', () => {
    expect(PostSchema.shape.createdAt.safeParse('2026-08-27T06:31:46').success).toBe(false);
  });
});
