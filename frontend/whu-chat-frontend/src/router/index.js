import { createRouter, createWebHistory } from 'vue-router'
import ChatRoom from '../views/ChatRoom.vue'
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import GroupChat from '../views/GroupChat.vue'
import Home from '../views/Home.vue'
import AIChat from '@/components/AIChat.vue'
import Discussion from '../views/Discussion.vue'
import MemberBenefits from '../views/MemberBenefits.vue'


const routes = [
  {
    path: '/',
    redirect: '/home'
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
    props: route => ({ 
      userId: Number(route.query.userId), 
      username: route.query.username 
    }),
    meta: { requiresAuth: true } // 需要登录才能访问
  },
  {
    path: '/discussion',
    name: 'Discussion',
    component: Discussion,
    meta: { requiresAuth: true } // 需要登录才能访问
  },
  {
    path: '/private-chat',
    name: 'PrivateChatList',
    component: () => import('../views/PrivateChat.vue'),
    meta: { requiresAuth: true } // 需要登录才能访问
  },
  {
    path: '/private-chat/:id',
    name: 'PrivateChat',
    component: () => import('../views/PrivateChat.vue'),
    meta: { requiresAuth: true } // 需要登录才能访问
  },
  {
    path: '/member-benefits',
    name: 'MemberBenefits',
    component: MemberBenefits,
    meta: { requiresAuth: true } // 需要登录才能访问
  }
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
})

// 全局导航守卫，用于检查是否需要登录
router.beforeEach((to, from, next) => {
  // 检查目标路由是否需要认证
  if (to.matched.some(record => record.meta.requiresAuth)) {
    // 这个路由需要认证，检查是否有token
    if (!localStorage.getItem('token')) {
      // 如果没有，重定向到登录页面
      next({ path: '/login' })
    } else {
      // 已登录，允许访问
      next()
    }
  } else {
    // 不需要认证的路由，直接放行
    next()
  }
})

export default router
