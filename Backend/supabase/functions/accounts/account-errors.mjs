// Return actionable messages without exposing provider details or account existence.
export function signupFailure(error) {
  const code = String(error?.code ?? "");
  const message = String(error?.message ?? "");
  if (/smtp|sending.*email|email.*sending/i.test(message)) {
    return { status: 503, error: "We could not send the confirmation email. The game's email service needs attention. Please try again after it is fixed." };
  }
  if (error?.status === 429 || code === "over_email_send_rate_limit" || code === "over_request_rate_limit") {
    return { status: 429, error: "Too many account or email requests. Please wait a few minutes before trying again." };
  }
  if (Number(error?.status) >= 500) {
    return { status: 503, error: "The account service is temporarily unavailable. Please try again later." };
  }
  return { status: 400, error: "Account could not be created. Check the details or sign in instead." };
}
