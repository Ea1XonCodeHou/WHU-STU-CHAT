import Login from "@/views/Login.vue";
import Register from "@/views/Register.vue";
import { createRouter, createWebHistory } from "vue-router";

// 定义需要登录权限的路由

const routes = [
    { path: '/', component: Login },
    { path: '/login', component: Login },
    { path: '/register', component: Register },
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;
