import axios from "axios"

function getToken() {
  return (
    localStorage.getItem("dataforge_token") ??
    sessionStorage.getItem("dataforge_token")
  )
}

function clearSession() {
  localStorage.removeItem("dataforge_token")
  localStorage.removeItem("dataforge_user")

  sessionStorage.removeItem("dataforge_token")
  sessionStorage.removeItem("dataforge_user")
}

export const api = axios.create({
  baseURL: "http://localhost:5021/api",
  headers: {
    "Content-Type": "application/json",
  },
})

api.interceptors.request.use((config) => {
  const token = getToken()

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      clearSession()

      if (
        window.location.pathname !== "/login" &&
        window.location.pathname !== "/registar"
      ) {
        window.location.href = "/login"
      }
    }

    return Promise.reject(error)
  },
)
