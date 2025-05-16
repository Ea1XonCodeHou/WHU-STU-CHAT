<template>
  <div class="modal-backdrop" @click.self="closeModal">
    <div class="modal-content">
      <div class="modal-header">
        <h3>设置</h3>
        <button class="close-button" @click="closeModal">
          <i class="fas fa-times"></i>
        </button>
      </div>
      
      <div class="modal-body">
        <div class="settings-section">
          <h4 class="section-title">用户界面设置</h4>
          
          <div class="setting-item">
            <span class="setting-label">深色模式</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.darkMode">
              <span class="slider"></span>
            </label>
          </div>
          
          <div class="setting-item">
            <span class="setting-label">消息提示音</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.messageSound">
              <span class="slider"></span>
            </label>
          </div>
          
          <h4 class="section-title">隐私设置</h4>
          
          <div class="setting-item">
            <span class="setting-label">显示在线状态</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.showMyOnlineStatus">
              <span class="slider"></span>
            </label>
          </div>
          
          <div class="setting-item">
            <span class="setting-label">消息通知</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.newMessageNotification">
              <span class="slider"></span>
            </label>
          </div>
          
          <h4 class="section-title">消息提醒设置</h4>
          
          <div class="setting-item">
            <span class="setting-label">私聊消息免打扰</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.privateChatMute">
              <span class="slider"></span>
            </label>
          </div>
          
          <div class="setting-item">
            <span class="setting-label">群聊消息免打扰</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.groupChatMute">
              <span class="slider"></span>
            </label>
          </div>
          
          <div class="setting-item">
            <span class="setting-label">系统通知免打扰</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.systemNotificationMute">
              <span class="slider"></span>
            </label>
          </div>
          
          <div class="form-actions">
            <button 
              @click="saveSettings" 
              class="save-button" 
              :disabled="isSaving"
            >
              <i v-if="isSaving" class="fas fa-spinner fa-spin"></i>
              <span v-else>保存设置</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, watch } from 'vue';
import axios from 'axios';

export default {
  name: 'UserSettings',
  props: {
    visible: {
      type: Boolean,
      required: true
    }
  },
  emits: ['close', 'settings-updated'],
  setup(props, { emit }) {
    const userId = ref(localStorage.getItem('userId') || '');
    
    // 设置项
    const settings = ref({
      darkMode: localStorage.getItem('setting_darkMode') === 'true',
      messageSound: localStorage.getItem('setting_messageSound') === 'true',
      showMyOnlineStatus: localStorage.getItem('setting_showMyOnlineStatus') === 'true',
      newMessageNotification: localStorage.getItem('setting_newMessageNotification') === 'true',
      privateChatMute: localStorage.getItem('setting_privateChatMute') === 'true',
      groupChatMute: localStorage.getItem('setting_groupChatMute') === 'true',
      systemNotificationMute: localStorage.getItem('setting_systemNotificationMute') === 'true'
    });
    
    // 原始设置（用于取消操作）
    const originalSettings = ref({});
    
    // 保存按钮状态
    const isSaving = ref(false);
    
    // 关闭模态框
    const closeModal = () => {
      // 重置为原来的设置，不应用修改
      if (userId.value) {
        loadSettings();
      }
      emit('close');
    };
    
    // 从服务器加载设置
    const loadSettings = async () => {
      try {
        // 确保我们有用户ID
        if (!userId.value) return;
        
        console.log(`正在加载用户 ${userId.value} 的设置...`);
        
        // 使用GET请求获取所有用户设置
        const response = await axios.get(`/api/user/${userId.value}/settings`);
        const userSettings = response.data;
        
        // 处理返回的设置
        if (userSettings) {
          // 将服务器设置值映射到本地设置对象
          const mappings = {
            'setting_darkMode': 'darkMode',
            'setting_messageSound': 'messageSound',
            'setting_showMyOnlineStatus': 'showMyOnlineStatus',
            'setting_newMessageNotification': 'newMessageNotification',
            'setting_privateChatMute': 'privateChatMute',
            'setting_groupChatMute': 'groupChatMute',
            'setting_systemNotificationMute': 'systemNotificationMute'
          };
          
          // 遍历服务器返回的所有设置
          Object.entries(userSettings).forEach(([key, value]) => {
            const localKey = mappings[key];
            if (localKey && settings.value.hasOwnProperty(localKey)) {
              // 将字符串的 "true"/"false" 转换为布尔值
              settings.value[localKey] = value === 'true';
            }
          });
          
          console.log('设置加载成功:', settings.value);
        }
      } catch (error) {
        console.error('加载设置失败:', error);
      }
    };
    
    // 应用设置效果
    const applySettings = async () => {
      console.log('正在应用设置:', settings.value);
      
      // 1. 应用深色模式
      if (settings.value.darkMode) {
        document.documentElement.classList.add('dark-mode');
        document.body.classList.add('dark-mode');
      } else {
        document.documentElement.classList.remove('dark-mode');
        document.body.classList.remove('dark-mode');
      }
      localStorage.setItem('setting_darkMode', settings.value.darkMode.toString());
      
      // 2. 设置消息声音通知
      window.allowMessageSound = settings.value.messageSound;
      localStorage.setItem('setting_messageSound', settings.value.messageSound.toString());
      
      // 3. 设置在线状态可见性
      localStorage.setItem('setting_showMyOnlineStatus', settings.value.showMyOnlineStatus.toString());
      // 将在线状态变更发送到后端
      await updateOnlineStatusVisibility(settings.value.showMyOnlineStatus);
      
      // 4. 设置消息通知
      window.allowNotifications = settings.value.newMessageNotification;
      localStorage.setItem('setting_newMessageNotification', settings.value.newMessageNotification.toString());
      
      // 5. 如果设置了消息通知，请求通知权限
      if (settings.value.newMessageNotification && Notification && Notification.permission !== 'granted' && Notification.permission !== 'denied') {
        try {
          const permission = await Notification.requestPermission();
          console.log('通知权限:', permission);
        } catch (error) {
          console.error('请求通知权限失败:', error);
        }
      }
      
      // 6. 设置消息免打扰
      localStorage.setItem('setting_privateChatMute', settings.value.privateChatMute.toString());
      localStorage.setItem('setting_groupChatMute', settings.value.groupChatMute.toString());
      localStorage.setItem('setting_systemNotificationMute', settings.value.systemNotificationMute.toString());
      window.privateChatMute = settings.value.privateChatMute;
      window.groupChatMute = settings.value.groupChatMute;
      window.systemNotificationMute = settings.value.systemNotificationMute;
      
      // 7. 触发设置更新事件
      emit('settings-updated', settings.value);
    };
    
    // 更新用户在线状态可见性
    const updateOnlineStatusVisibility = async (isVisible) => {
      try {
        const userId = localStorage.getItem('userId');
        if (!userId) {
          console.error('用户ID不存在，无法更新在线状态');
          return;
        }
        
        // 发送请求更新用户在线状态可见性
        await axios.post(`/api/user/${userId}/status`, {
          userId: userId,
          isOnline: true, // 用户当前在线
          isVisible: isVisible // 是否显示在线状态
        });
        
        console.log('用户在线状态可见性已更新:', isVisible);
      } catch (error) {
        console.error('更新用户在线状态可见性失败:', error);
      }
    };
    
    // 保存设置
    const saveSettings = async () => {
      isSaving.value = true;
      
      try {
        // 应用设置效果
        await applySettings();
        
        // 保存每项设置到数据库
        for (const [key, value] of Object.entries(settings.value)) {
          const settingKey = `setting_${key}`;
          await axios.post(`/api/user/${userId.value}/settings`, {
            userId: userId.value,
            settingKey: settingKey,
            settingValue: value.toString()
          });
        }
        
        // 测试通知效果
        if (settings.value.messageSound) {
          window.playMessageSound();
        }
        
        if (settings.value.newMessageNotification) {
          window.showNotification('设置已保存', '你的设置已成功保存并生效');
        }
        
        alert('设置已保存');
      } catch (error) {
        console.error('保存设置失败:', error);
        alert('保存设置失败: ' + error.message);
      } finally {
        isSaving.value = false;
      }
    };
    
    // 组件挂载时
    onMounted(() => {
      // 加载服务器设置
      if (userId.value) {
        loadSettings();
      }
    });
    
    return {
      settings,
      isSaving,
      closeModal,
      saveSettings
    };
  }
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  width: 500px;
  max-width: 90%;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  max-height: 80vh;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.modal-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.close-button {
  background: transparent;
  border: none;
  color: white;
  font-size: 16px;
  cursor: pointer;
  padding: 5px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  width: 30px;
  height: 30px;
  transition: background-color 0.2s;
}

.close-button:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

.modal-body {
  padding: 20px;
  overflow-y: auto;
  max-height: calc(80vh - 60px);
}

.settings-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  color: #4776E6;
  margin-bottom: 10px;
  padding-bottom: 5px;
  border-bottom: 1px solid #eee;
}

.setting-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 0;
}

.setting-label {
  font-size: 14px;
  color: #333;
}

/* 开关样式 */
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 44px;
  height: 22px;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #ccc;
  transition: .4s;
  border-radius: 22px;
}

.slider:before {
  position: absolute;
  content: "";
  height: 18px;
  width: 18px;
  left: 3px;
  bottom: 2px;
  background-color: white;
  transition: .4s;
  border-radius: 50%;
}

input:checked + .slider {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
}

input:focus + .slider {
  box-shadow: 0 0 1px #4776E6;
}

input:checked + .slider:before {
  transform: translateX(20px);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

.save-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 100px;
  transition: transform 0.2s, box-shadow 0.2s;
}

.save-button:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.3);
}

.save-button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style> 