import { fileURLToPath, URL } from 'node:url'

import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    //这实际上并不是简单地"替换"路径前缀，而是进行了如下操作：
    // 当请求匹配到以 /admin 开头的路径时，代理会被激活
    // 完整的请求路径会被保留，但请求会被转发到 target 指定的目标服务器
    // 所以 /admin/teacher_login 被代理后变成了 http://localhost:8080/admin/teacher_login
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        secure: false
      },
      '/admin': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
