import { ApiError, NETWORK_ERROR_CODE } from "@/lib/api/errors";

const FALLBACK_MESSAGE = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";

const MESSAGES_BY_CODE: Record<string, string> = {
  [NETWORK_ERROR_CODE]: "Sunucuya ulaşılamıyor. Bağlantınızı kontrol edip tekrar deneyin.",
  validation_failed: "Lütfen formdaki bilgileri kontrol edin.",
  bad_request: "İstek işlenemedi. Lütfen bilgileri kontrol edin.",
  unauthorized: "Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.",
  forbidden: "Bu işlem için yetkiniz yok.",
  not_found: "Aradığınız kayıt bulunamadı.",
  conflict: "Bu kayıt zaten mevcut.",
  business_rule_violation: "Bu işlem şu anda gerçekleştirilemiyor.",
  internal_error: "Sunucuda bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
  "auth.invalid_credentials": "E-posta veya şifre hatalı.",
  "auth.invalid_refresh_token": "Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.",
  "auth.account_disabled": "Bu hesap devre dışı bırakılmış. Şirket yöneticinizle iletişime geçin.",
  "auth.email_already_in_use": "Bu e-posta adresiyle kayıtlı bir hesap zaten var.",
  "auth.invalid_token": "Bağlantı geçersiz veya süresi dolmuş.",
  insufficient_privileges: "Sahip olmadığınız izinleri başkasına veremez veya değiştiremezsiniz.",
  "users.email_not_verified": "Kullanıcı davet etmeden önce e-posta adresinizi doğrulayın.",
  "users.cannot_modify_self": "Kendi rolünüzü veya hesap durumunuzu değiştiremezsiniz.",
  "users.last_admin": "Şirkette en az bir aktif Admin kalmalıdır.",
  "users.invalid_roles": "Seçilen rollerden biri artık mevcut değil. Sayfayı yenileyip tekrar deneyin.",
  "users.invitation_already_accepted": "Bu kullanıcının bekleyen bir daveti yok.",
  "roles.system_role_locked": "Sistem rolleri değiştirilemez ve silinemez.",
  "roles.in_use": "Bu rol kullanıcılara atanmış. Silmeden önce bu kullanıcıların rolünü değiştirin.",
  "roles.name_taken": "Bu isimde bir rol zaten var.",
  "roles.invalid_permissions": "Seçilen izinlerden biri geçerli değil.",
  "projects.key_taken": "Bu kısa adı kullanan bir proje zaten var.",
  "projects.member_exists": "Bu kullanıcı zaten projenin üyesi.",
  "projects.invalid_member": "Bu kullanıcı projeye eklenemez. Hesabı pasif ya da silinmiş olabilir.",
  "projects.last_lead": "Projede en az bir aktif proje lideri kalmalıdır.",
};

function tooManyRequestsMessage(retryAfterSeconds: number | null): string {
  if (retryAfterSeconds === null) {
    return "Çok fazla deneme yaptınız. Lütfen biraz bekleyip tekrar deneyin.";
  }

  return `Çok fazla deneme yaptınız. Lütfen ${retryAfterSeconds} saniye sonra tekrar deneyin.`;
}

export function getErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return FALLBACK_MESSAGE;
  }

  if (error.code === "too_many_requests" || error.status === 429) {
    return tooManyRequestsMessage(error.retryAfterSeconds);
  }

  return MESSAGES_BY_CODE[error.code] ?? FALLBACK_MESSAGE;
}
