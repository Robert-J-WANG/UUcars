import { Route, Routes } from "react-router-dom";
import LoginPage from "./pages/LoginPage";

function App() {
  return (
    <>
      <Routes>
        <Route path={"/login"} element={<LoginPage />} />
        <Route path="/" element={<div className="p-8">首页（占位）</div>} />
      </Routes>
    </>
  );
}

export default App;
