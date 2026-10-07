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
