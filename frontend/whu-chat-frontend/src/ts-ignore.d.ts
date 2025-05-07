// 用于忽略项目中缺失文件的类型声明
declare module '*/PrivateChat.vue' {
  import { defineComponent } from 'vue'
  const component: ReturnType<typeof defineComponent>
  export default component
} 