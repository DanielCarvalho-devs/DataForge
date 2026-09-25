import {
  Navigate,
  Route,
  Routes,
} from "react-router-dom"

import { ProtectedRoute } from "./auth/ProtectedRoute"
import { AppLayout } from "./components/layout/AppLayout"

import { DatasetWorkspacePage } from "./pages/DatasetWorkspacePage"
import { HomePage } from "./pages/HomePage"
import { LoginPage } from "./pages/LoginPage"
import { ProjectsPage } from "./pages/ProjectsPage"
import { ProjectWorkspacePage } from "./pages/ProjectWorkspacePage"
import { RegisterPage } from "./pages/RegisterPage"
import { ProfilePage } from "./pages/ProfilePage"

export default function App() {
  return (
    <Routes>
      <Route
        path="/login"
        element={<LoginPage />}
      />

      <Route
        path="/registar"
        element={<RegisterPage />}
      />

      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route
          index
          element={<HomePage />}
        />

        <Route
          path="projetos"
          element={<ProjectsPage />}
        />

        <Route
          path="projetos/:id"
          element={<ProjectWorkspacePage />}
        />

        <Route
          path="datasets/:id"
          element={<DatasetWorkspacePage />}
        />
              <Route path="/perfil" element={<ProfilePage />} />
</Route>

      <Route
        path="*"
        element={
          <Navigate
            to="/"
            replace
          />
        }
      />
    </Routes>
  )
}

