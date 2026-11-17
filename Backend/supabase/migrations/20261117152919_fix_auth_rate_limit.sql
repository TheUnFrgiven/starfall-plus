-- Qualify the conflict constraint so the bucket parameter cannot shadow it.
create or replace function public.allow_auth(bucket text) returns boolean language plpgsql security definer set search_path='' as $$
declare n int; begin
 delete from private.auth_windows where minute<now()-interval '1 day';
 insert into private.auth_windows values(bucket,date_trunc('minute',now()),1) on conflict on constraint auth_windows_pkey do update set hits=private.auth_windows.hits+1 returning hits into n;
 return n<=8;
end $$;
