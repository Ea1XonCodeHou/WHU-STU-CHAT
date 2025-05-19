// 导入依赖
import { createStore } from 'vuex'
import createPersistedState from 'vuex-persistedstate'

// 用户信息模块
const user = {
  namespaced: true,
  state: () => ({
    userInfo: null,
    token: null,
    isAuthenticated: false
  }),
  mutations: {
    SET_USER_INFO(state, userInfo) {
      state.userInfo = userInfo
    },
    SET_TOKEN(state, token) {
      state.token = token
      state.isAuthenticated = !!token
    },
    CLEAR_USER(state) {
      state.userInfo = null
      state.token = null
      state.isAuthenticated = false
    },
    UPDATE_USER_LEVEL(state, level) {
      if (state.userInfo) {
        state.userInfo.level = level
      }
    }
  },
  actions: {
    loginUser({ commit }, { userData, token }) {
      // 保存用户信息到本地存储作为备份
      localStorage.setItem('userInfo', JSON.stringify(userData))
      localStorage.setItem('token', token)
      
      // 更新store状态
      commit('SET_USER_INFO', userData)
      commit('SET_TOKEN', token)
    },
    updateUserLevel({ commit }, level) {
      commit('UPDATE_USER_LEVEL', level)
      
      // 同步到本地存储
      const userInfo = JSON.parse(localStorage.getItem('userInfo') || '{}')
      userInfo.level = level
      localStorage.setItem('userInfo', JSON.stringify(userInfo))
    },
    logoutUser({ commit }) {
      // 清除本地存储
      localStorage.removeItem('userInfo')
      localStorage.removeItem('token')
      
      // 清除状态
      commit('CLEAR_USER')
    },
    initializeStore({ commit }) {
      // 从本地存储初始化状态
      const token = localStorage.getItem('token')
      const userInfo = localStorage.getItem('userInfo')
      
      if (token) {
        commit('SET_TOKEN', token)
      }
      
      if (userInfo) {
        try {
          commit('SET_USER_INFO', JSON.parse(userInfo))
        } catch (error) {
          console.error('解析存储的用户信息失败', error)
        }
      }
    }
  },
  getters: {
    isAuthenticated: state => state.isAuthenticated,
    userInfo: state => state.userInfo || {},
    userId: state => state.userInfo?.id || null,
    username: state => state.userInfo?.username || '',
    userLevel: state => state.userInfo?.level || 0,
    avatar: state => state.userInfo?.avatar || ''
  }
}

// 创建store
export default createStore({
  modules: {
    user
  },
  // 使用插件持久化存储
  plugins: [
    createPersistedState({
      key: 'whu-chat-state',
      paths: ['user']
    })
  ]
}) 