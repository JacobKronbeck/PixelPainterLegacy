<template>
  <Button
    :label="isLoggedIn ? 'Account' : 'Login'"
    rounded
    @click="buttonClick()"
    icon="pi pi-google"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import { onBeforeUnmount, onMounted, ref } from "vue";
import router from "@/router";
import LoginService from "@/services/LoginService";
import Artist from "@/entities/Artist";
import { apiUrl } from "@/services/apiClient";

const isLoggedIn = ref<boolean>(false);
const currentUser = ref<Artist | null>(null);

function handleUsernameUpdated(event: Event): void {
  const updatedName = (event as CustomEvent<{ name?: string }>).detail?.name;
  if (currentUser.value && updatedName) {
    currentUser.value.name = updatedName;
  }
}

onMounted(async () => {
  window.addEventListener("pixel-painter:username-updated", handleUsernameUpdated);
  try {
    currentUser.value = await LoginService.getCurrentUser();
    isLoggedIn.value = true;
  } catch {
    currentUser.value = null;
    isLoggedIn.value = false;
  }
});

onBeforeUnmount(() => {
  window.removeEventListener("pixel-painter:username-updated", handleUsernameUpdated);
});

async function buttonClick(): Promise<void> {
  if (isLoggedIn.value) {
    const user = currentUser.value ?? await LoginService.getCurrentUser();
    await router.push(`/accountpage/${encodeURIComponent(user.name)}#created_art`);
  } else {
    login();
  }
}

function login(): void {
  window.location.replace(apiUrl("/api/v2/auth/login"));
}
</script>
