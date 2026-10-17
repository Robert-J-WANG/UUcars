import { GoogleOAuthProvider } from "@react-oauth/google";
import { Outlet } from "react-router-dom";

export default function GoogleAuthLayout() {
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

  if (!clientId) {
    throw new Error("VITE_GOOGLE_CLIENT_ID is not configured");
  }

  return (
    <GoogleOAuthProvider clientId={clientId}>
      <Outlet />
    </GoogleOAuthProvider>
  );
}
