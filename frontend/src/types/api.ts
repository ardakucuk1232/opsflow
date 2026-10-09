export type AuthUser = {
  id: string;
  companyId: string;
  companyName: string;
  email: string;
  isEmailVerified: boolean;
  firstName: string;
  lastName: string;
  roles: string[];
  permissions: string[];
};

export type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
};

export type RoleReference = {
  id: string;
  name: string;
};

export type UserSummary = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  isEmailVerified: boolean;
  invitationPending: boolean;
  roles: RoleReference[];
  lastLoginAt: string | null;
  createdAt: string;
};

export type Role = {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  permissions: string[];
  userCount: number;
};

export type Permission = {
  code: string;
  group: string;
  description: string;
};

export type InvitationPreview = {
  email: string;
  firstName: string;
  lastName: string;
  companyName: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};
