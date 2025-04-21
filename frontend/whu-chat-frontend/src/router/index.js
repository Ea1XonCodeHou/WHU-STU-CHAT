import { createRouter, createWebHistory } from 'vue-router'
import ChatRoom from '../views/ChatRoom.vue'
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'

const routes = [
  {
    path: '/',
    redirect: '/login'
  },
  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: { requiresAuth: false }
  },
  {
    path: '/register',
    name: 'Register',
    component: Register,
    meta: { requiresAuth: false }
  },
  {
    path: '/chatroom',
    name: 'ChatRoom',
    component: ChatRoom,
    meta: { requiresAuth: true } // 需要登录才能访问
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 全局路由守卫，已登录用户访问登录页时自动重定向到聊天室
router.beforeEach((to, from, next) => {
  // 检查本地存储中是否有token
  const isLoggedIn = !!localStorage.getItem('token');
  
  // 如果需要登录但用户未登录，则重定向到登录页
  if (to.meta.requiresAuth && !isLoggedIn) {
    next({ path: '/login' });
  }
  // 如果已登录且访问登录页或注册页，重定向到聊天室
  else if (isLoggedIn && (to.path === '/login' || to.path === '/register')) {
    next({ path: '/chatroom' });
  } else {
    next();
  }
});

export default router
