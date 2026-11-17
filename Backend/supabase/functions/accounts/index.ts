import { createClient } from "npm:@supabase/supabase-js@2.119.0";
import { signupFailure } from "./account-errors.mjs";

const url = Deno.env.get("SUPABASE_URL")!;
const key = Object.values(JSON.parse(Deno.env.get("SUPABASE_PUBLISHABLE_KEYS") ?? "{}") as Record<string,string>)[0] ?? Deno.env.get("SUPABASE_ANON_KEY")!;
const secret = Object.values(JSON.parse(Deno.env.get("SUPABASE_SECRET_KEYS") ?? "{}") as Record<string,string>)[0] ?? Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
const pepper = Deno.env.get("AUTH_RATE_PEPPER") ?? secret;
const recoveryRedirect = Deno.env.get("ACCOUNT_REDIRECT_URL");
const origins = new Set([new URL(url).origin, ...(Deno.env.get("ALLOWED_ORIGINS") ?? "").split(",").filter(Boolean)]);
const options = { auth: { persistSession: false, autoRefreshToken: false, detectSessionInUrl: false } };

Deno.serve(async (request) => {
  const origin = request.headers.get("origin");
  const headers: Record<string,string> = { "Content-Type": "application/json", "Cache-Control": "no-store", "Access-Control-Allow-Headers": "authorization, apikey, content-type", "Access-Control-Allow-Methods": "POST, OPTIONS" };
  if (origin && origins.has(origin)) headers["Access-Control-Allow-Origin"] = origin;
  const reply = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers });
  if (origin && !origins.has(origin)) return reply(403, { error: "Origin not allowed" });
  if (request.method === "OPTIONS") return new Response(null, {status:204,headers});
  if (request.method !== "POST") return reply(405, {error:"POST required"});
  if (!pepper || !recoveryRedirect) return reply(503, {error:"Account gateway needs server configuration"});
  try {
    const raw = await request.text(); if (raw.length > 4096) return reply(413,{error:"Request too large"});
    const body = JSON.parse(raw);
    const auth = createClient(url, key, options), admin = createClient(url, secret, options);
    const handle = String(body.username ?? "").trim().toLowerCase();
    const emailInput = String(body.email ?? "").trim().toLowerCase();
    const password = String(body.password ?? "");
    const ip = request.headers.get("x-forwarded-for")?.split(",")[0] ?? "native";
    const rateKey = await crypto.subtle.importKey("raw",new TextEncoder().encode(pepper),{name:"HMAC",hash:"SHA-256"},false,["sign"]);
    for (const source of [`ip:${ip}`,`account:${handle || emailInput}`]) {
      const digest = new Uint8Array(await crypto.subtle.sign("HMAC",rateKey,new TextEncoder().encode(source)));
      const bucket = Array.from(digest,x=>x.toString(16).padStart(2,"0")).join("");
      const {data,error} = await admin.rpc("allow_auth",{bucket});
      if (error) return reply(503,{error:"Account database setup needs attention"});
      if (!data) return reply(429,{error:"Please wait a minute and try again"});
    }
    if (body.action === "signup") {
      if (body.parentAccepted !== "yes" || !/^[a-z0-9_]{3,24}$/.test(handle) || !emailInput.includes("@") || password.length < 12 || password.length > 128) return reply(400,{error:"Use a parent email, a 3–24 character username and a password of at least 12 characters"});
      const {data,error} = await auth.auth.signUp({email:emailInput,password,options:{data:{username:handle},emailRedirectTo:recoveryRedirect}});
      if (error) { const failure = signupFailure(error); return reply(failure.status,{error:failure.error}); }
      return reply(200,{session:data.session,message:"Check the parent's email to confirm the account, then sign in."});
    }
    if (body.action === "login" || body.action === "recover") {
      let email = emailInput;
      if (!email && handle) { const result = await admin.rpc("account_email",{handle}); if(result.error) return reply(503,{error:"Please try again later"}); email = result.data ?? ""; }
      if (body.action === "recover") {
        if (email) await auth.auth.resetPasswordForEmail(email,{redirectTo:recoveryRedirect});
        return reply(200,{message:"If this account exists, a recovery email has been sent."});
      }
      const {data,error} = await auth.auth.signInWithPassword({email:email || "unknown@invalid.example",password});
      if (error || !data.session) return reply(401,{error:"Could not sign in. Check the details and confirmation email."});
      return reply(200,{session:data.session});
    }
    if (body.action === "change") {
      const token = request.headers.get("authorization")?.replace(/^Bearer\s+/i,"") ?? "";
      const {data:userData,error:whoError} = await auth.auth.getUser(token);
      if (whoError || !userData.user?.email) return reply(401,{error:"Sign in first"});
      if (password || emailInput && emailInput !== userData.user.email) {
        const {data:reauth,error:reauthError} = await auth.auth.signInWithPassword({email:userData.user.email,password:String(body.currentPassword ?? "")});
        if (reauthError || reauth.user?.id !== userData.user.id) return reply(401,{error:"Current password required. Google-only accounts can use password recovery."});
      }
      if (!/^[a-z0-9_]{3,24}$/.test(handle) || password && (password.length<12 || password.length>128)) return reply(400,{error:"Check username and new password"});
      const user = createClient(url,key,{...options,global:{headers:{Authorization:`Bearer ${token}`}}});
      const rename = await user.rpc("rename_account",{username:handle}); if(rename.error) return reply(400,{error:"Username is unavailable"});
      if (password || emailInput && emailInput !== userData.user.email) { const changed = await auth.auth.updateUser({...password ? {password} : {}, ...emailInput && emailInput !== userData.user.email ? {email:emailInput} : {}}); if(changed.error) return reply(400,{error:"Username saved. Email or password could not be changed; try again."}); }
      return reply(200,{message:"Account updated. Confirm any email change in the confirmation emails."});
    }
    return reply(400,{error:"Unknown account action"});
  } catch { return reply(400,{error:"Could not complete this request"}); }
});
