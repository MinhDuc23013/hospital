// NextAuth v5 route handler
// Handles all /api/auth/* routes: signin, signout, callback, session, csrf
import { handlers } from "@/lib/auth-config";

export const { GET, POST } = handlers;
