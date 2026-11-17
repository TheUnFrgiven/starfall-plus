import { test } from "node:test";
import assert from "node:assert/strict";
import { signupFailure } from "../supabase/functions/accounts/account-errors.mjs";

test("mail rejection gives service guidance rather than blaming account details", () => {
  const result = signupFailure({status:500, code:"unexpected_failure", message:"Error sending confirmation email: private provider detail"});
  assert.equal(result.status, 503);
  assert.match(result.error, /confirmation email/);
  assert.doesNotMatch(result.error, /private provider detail/);
});
test("signup failures distinguish service and rate limits without revealing existing accounts", () => {
  assert.equal(signupFailure({status:429}).status, 429);
  assert.equal(signupFailure({status:500, message:"Database error saving new user"}).status, 503);
  assert.equal(signupFailure({status:422, code:"user_already_exists"}).error, signupFailure({status:400}).error);
});
