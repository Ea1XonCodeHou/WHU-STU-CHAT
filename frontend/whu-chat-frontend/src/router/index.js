import { createRouter, createWebHistory } from 'vue-router'
import ChatRoom from '../views/ChatRoom.vue'
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'

import GroupChat from '../views/GroupChat.vue'

import Home from '../views/Home.vue'
import AIChat from '@/components/AIChat.vue'


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
    path: '/home',
    name: 'Home',
    component: Home,
    meta: { requiresAuth: true } // 需要登录才能访问
  },
  {
    path: '/chatroom/:id',
    name: 'ChatRoom',
    component: ChatRoom,
    props: true,
    meta: { requiresAuth: true } // 需要登录才能访问
  },
  {

    path: '/groupchat',
    name: 'GroupChat',
    component: GroupChat,
    meta: { requiresAuth: true } // 需要登录才能访问
  },

  {
    path: '/chat',
    name: 'AIChat',
    component: AIChat,
    meta: {
      requiresAuth: true
    },
    props: route => ({
      userId: Number(localStorage.getItem('userId')),
      username: localStorage.getItem('username')
    })

  },
  {
    path: '/private-chat/:id',
    name: 'PrivateChat',
    component: () => import('../views/PrivateChat.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 全局路由守卫，已登录用户访问登录页时自动重定向到主页
router.beforeEach((to, from, next) => {
  // 检查本地存储中是否有token
  const isLoggedIn = !!localStorage.getItem('token');
  
  // 如果需要登录但用户未登录，则重定向到登录页
  if (to.meta.requiresAuth && !isLoggedIn) {
    next({ path: '/login' });
  }
  // 如果已登录且访问登录页或注册页，重定向到主页
  else if (isLoggedIn && (to.path === '/login' || to.path === '/register')) {
    next({ path: '/home' });
  } else {
    next();
  }
});

export default router
